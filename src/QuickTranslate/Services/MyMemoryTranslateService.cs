using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class MyMemoryTranslateService : ITranslationService
{
    public TranslationProvider Provider => TranslationProvider.MyMemory;

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MyMemoryTranslateService> _logger;

    private const string BaseUrl = "https://api.mymemory.translated.net/get";
    private const int MaxRetries = 2;

    public MyMemoryTranslateService(
        HttpClient httpClient,
        ISettingsService settingsService,
        ILogger<MyMemoryTranslateService> logger)
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

        var langPair = normalizedSource.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? $"autodetect|{normalizedTarget}"
            : $"{normalizedSource}|{normalizedTarget}";

        var timeoutSeconds = _settingsService.CurrentSettings?.RequestTimeoutSeconds ?? 5;
        if (timeoutSeconds < 2) timeoutSeconds = 2;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting MyMemory translation of {CharCount} characters from '{Source}' to '{Target}' (timeout: {Timeout}s)",
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
                var content = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("q", text),
                    new KeyValuePair<string, string>("langpair", langPair)
                ]);

                using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
                {
                    Content = content
                };
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                using var response = await _httpClient.SendAsync(request, linkedCts.Token);

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    _logger.LogWarning("MyMemory rate limit reached (HTTP 429)");
                    return TranslationResult.Failure(text, "Превышен бесплатный лимит запросов MyMemory. Попробуйте другой сервис.", normalizedTarget);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning("MyMemory returned HTTP {StatusCode}", statusCode);

                    if (attempt <= MaxRetries && (statusCode >= 500 || statusCode == 408))
                    {
                        await Task.Delay(250 * attempt, linkedCts.Token);
                        continue;
                    }

                    return TranslationResult.Failure(
                        text,
                        $"Ошибка сервиса MyMemory (HTTP {statusCode}).",
                        normalizedTarget);
                }

                var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

                if (responseBody.Contains("MYMEMORY WARNING: YOU USED ALL YOUR AVAILABLE FREE BANDWIDTH", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("MyMemory daily bandwidth exceeded");
                    return TranslationResult.Failure(text, "Исчерпан дневной лимит бесплатных запросов MyMemory. Выберите Google или Yandex в настройках.", normalizedTarget);
                }

                var result = ParseMyMemoryResponse(text, responseBody, normalizedTarget);

                stopwatch.Stop();
                _logger.LogInformation(
                    "MyMemory translation completed successfully in {ElapsedMilliseconds} ms (detected language: {DetectedLanguage})",
                    stopwatch.ElapsedMilliseconds, result.DetectedSourceLanguage ?? "unknown");

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("MyMemory translation timed out after {Timeout} seconds", timeoutSeconds);
                return TranslationResult.Failure(text, "Время ожидания ответа сервиса перевода истекло.", normalizedTarget);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MyMemory translation was cancelled by caller");
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
                _logger.LogError(ex, "Failed to parse MyMemory response JSON");
                return TranslationResult.Failure(text, "Получен некорректный ответ от сервиса перевода.", normalizedTarget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during MyMemory translation");
                return TranslationResult.Failure(text, $"Ошибка перевода: {ex.Message}", normalizedTarget);
            }
        }

        if (lastException != null)
        {
            return TranslationResult.Failure(text, "Отсутствует подключение к интернету или сервис перевода недоступен.", normalizedTarget);
        }

        return TranslationResult.Failure(text, "Не удалось выполнить перевод.", normalizedTarget);
    }

    public static TranslationResult ParseMyMemoryResponse(string originalText, string json, string targetLanguage)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Invalid root element in MyMemory response");
        }

        if (root.TryGetProperty("responseStatus", out var statusElement))
        {
            var status = statusElement.GetInt32();
            if (status != 200 && status != 0)
            {
                var details = root.TryGetProperty("responseDetails", out var detailsElem)
                    ? detailsElem.GetString()
                    : null;
                return TranslationResult.Failure(originalText, details ?? $"Ошибка сервиса MyMemory ({status}).", targetLanguage);
            }
        }

        if (!root.TryGetProperty("responseData", out var responseData) || responseData.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Missing responseData property in MyMemory response");
        }

        if (!responseData.TryGetProperty("translatedText", out var translatedElement))
        {
            throw new JsonException("Missing translatedText property in MyMemory response");
        }

        var rawTranslated = translatedElement.GetString() ?? string.Empty;
        var translatedText = WebUtility.HtmlDecode(rawTranslated);
        if (string.IsNullOrEmpty(translatedText))
        {
            translatedText = originalText;
        }

        string? detectedLanguage = null;
        if (responseData.TryGetProperty("detectedLanguage", out var detectedElem) && detectedElem.ValueKind == JsonValueKind.String)
        {
            detectedLanguage = detectedElem.GetString();
        }

        return TranslationResult.Success(originalText, translatedText, detectedLanguage, targetLanguage);
    }
}
