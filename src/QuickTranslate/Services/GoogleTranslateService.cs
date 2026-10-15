using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class GoogleTranslateService : ITranslationService
{
    public TranslationProvider Provider => TranslationProvider.Google;

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GoogleTranslateService> _logger;
    private const string BaseUrl = "https://translate.googleapis.com/translate_a/single";
    private const int MaxRetries = 2;

    public GoogleTranslateService(
        HttpClient httpClient,
        ISettingsService settingsService,
        ILogger<GoogleTranslateService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TranslationResult.Failure(text, "Текст для перевода отсутствует.", targetLanguage);
        }

        var normalizedSource = string.IsNullOrWhiteSpace(sourceLanguage) ? "auto" : sourceLanguage.Trim();
        var normalizedTarget = string.IsNullOrWhiteSpace(targetLanguage) ? "ru" : targetLanguage.Trim();

        var timeoutSeconds = _settingsService.CurrentSettings?.RequestTimeoutSeconds ?? 5;
        if (timeoutSeconds < 2) timeoutSeconds = 2;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting translation of {CharCount} characters from '{Source}' to '{Target}' (timeout: {Timeout}s)",
            text.Length, normalizedSource, normalizedTarget, timeoutSeconds);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxRetries + 1; attempt++)
        {
            if (linkedCts.Token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var url = $"{BaseUrl}?client=gtx&sl={Uri.EscapeDataString(normalizedSource)}&tl={Uri.EscapeDataString(normalizedTarget)}&dt=t";
                var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("q", text)]);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                using var response = await _httpClient.SendAsync(request, linkedCts.Token);

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    _logger.LogWarning("Google Translate rate limit reached (HTTP 429)");
                    return TranslationResult.Failure(text, "Превышен лимит запросов Google Translate. Попробуйте позже.", normalizedTarget);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning("Google Translate returned HTTP {StatusCode}", statusCode);

                    if (attempt <= MaxRetries && (statusCode >= 500 || statusCode == 408))
                    {
                        await Task.Delay(250 * attempt, linkedCts.Token);
                        continue;
                    }

                    return TranslationResult.Failure(
                        text,
                        $"Ошибка сервиса перевода (HTTP {statusCode}).",
                        normalizedTarget);
                }

                var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);
                var result = ParseGoogleResponse(text, responseBody, normalizedTarget);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Translation completed successfully in {ElapsedMilliseconds} ms (detected language: {DetectedLanguage})",
                    stopwatch.ElapsedMilliseconds, result.DetectedSourceLanguage ?? "unknown");

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Translation timed out after {Timeout} seconds", timeoutSeconds);
                return TranslationResult.Failure(text, "Время ожидания ответа сервиса перевода истекло.", normalizedTarget);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Translation was cancelled by caller");
                return TranslationResult.Failure(text, "Перевод был отменен.", normalizedTarget);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning("Network error on attempt {Attempt}/{MaxAttempts}: {Message}", attempt, MaxRetries + 1, ex.Message);

                if (attempt <= MaxRetries)
                {
                    try
                    {
                        await Task.Delay(300 * attempt, linkedCts.Token);
                        continue;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Google Translate response JSON");
                return TranslationResult.Failure(text, "Получен некорректный ответ от сервиса перевода.", normalizedTarget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during translation");
                return TranslationResult.Failure(text, $"Ошибка перевода: {ex.Message}", normalizedTarget);
            }
        }

        if (lastException != null)
        {
            return TranslationResult.Failure(text, "Отсутствует подключение к интернету или сервис перевода недоступен.", normalizedTarget);
        }

        return TranslationResult.Failure(text, "Не удалось выполнить перевод.", normalizedTarget);
    }

    public static TranslationResult ParseGoogleResponse(string originalText, string json, string targetLanguage)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            throw new JsonException("Invalid root element in translation response");
        }

        var translatedBuilder = new StringBuilder();
        var sentences = root[0];

        if (sentences.ValueKind == JsonValueKind.Array)
        {
            foreach (var sentence in sentences.EnumerateArray())
            {
                if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
                {
                    var part = sentence[0].GetString();
                    if (!string.IsNullOrEmpty(part))
                    {
                        translatedBuilder.Append(part);
                    }
                }
            }
        }

        var translatedText = translatedBuilder.ToString();
        if (string.IsNullOrEmpty(translatedText))
        {
            translatedText = originalText;
        }

        string? detectedLanguage = null;
        if (root.GetArrayLength() > 2 && root[2].ValueKind == JsonValueKind.String)
        {
            detectedLanguage = root[2].GetString();
        }

        return TranslationResult.Success(originalText, translatedText, detectedLanguage, targetLanguage);
    }
}
