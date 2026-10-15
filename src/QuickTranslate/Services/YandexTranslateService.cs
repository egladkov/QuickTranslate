using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class YandexTranslateService : ITranslationService
{
    public TranslationProvider Provider => TranslationProvider.Yandex;

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<YandexTranslateService> _logger;

    private const string BaseUrl = "https://translate.yandex.net/api/v1/tr.json/translate";
    private const string Srv = "ios";
    private const string Ucid = "9676696D-0B56-4F13-B4D5-4A3DA2A3344D";
    private const string Sid = "1A5A10A952AB4A3B82533F44B87EE696";
    private const string Id = "1A5A10A952AB4A3B82533F44B87EE696-0-0";
    private const int MaxRetries = 2;

    public YandexTranslateService(
        HttpClient httpClient,
        ISettingsService settingsService,
        ILogger<YandexTranslateService> logger)
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

        var normalizedSource = NormalizeLangCode(sourceLanguage);
        var normalizedTarget = NormalizeLangCode(targetLanguage);
        if (string.IsNullOrWhiteSpace(normalizedTarget))
        {
            normalizedTarget = "ru";
        }

        var langParam = string.IsNullOrWhiteSpace(normalizedSource) || normalizedSource == "auto"
            ? normalizedTarget
            : $"{normalizedSource}-{normalizedTarget}";

        var timeoutSeconds = _settingsService.CurrentSettings?.RequestTimeoutSeconds ?? 5;
        if (timeoutSeconds < 2) timeoutSeconds = 2;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting Yandex translation of {CharCount} characters from '{Source}' to '{Target}' (timeout: {Timeout}s)",
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
                var queryParams = $"srv={Srv}&ucid={Ucid}&sid={Sid}&id={Id}&lang={Uri.EscapeDataString(langParam)}";
                var url = $"{BaseUrl}?{queryParams}";
                var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("text", text)]);

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                request.Headers.Add("User-Agent", "ru.yandex.translate/21.14.1");

                using var response = await _httpClient.SendAsync(request, linkedCts.Token);

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    _logger.LogWarning("Yandex Translate rate limit reached (HTTP 429)");
                    return TranslationResult.Failure(text, "Превышен лимит запросов Yandex Translate. Попробуйте позже.", normalizedTarget);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning("Yandex Translate returned HTTP {StatusCode}", statusCode);

                    if (attempt <= MaxRetries && (statusCode >= 500 || statusCode == 408))
                    {
                        await Task.Delay(250 * attempt, linkedCts.Token);
                        continue;
                    }

                    return TranslationResult.Failure(
                        text,
                        $"Ошибка сервиса Yandex Translate (HTTP {statusCode}).",
                        normalizedTarget);
                }

                var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token);
                var result = ParseYandexResponse(text, responseBody, normalizedTarget);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Yandex translation completed successfully in {ElapsedMilliseconds} ms (detected language: {DetectedLanguage})",
                    stopwatch.ElapsedMilliseconds, result.DetectedSourceLanguage ?? "unknown");

                return result;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Yandex translation timed out after {Timeout} seconds", timeoutSeconds);
                return TranslationResult.Failure(text, "Время ожидания ответа сервиса перевода истекло.", normalizedTarget);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Yandex translation was cancelled by caller");
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
                _logger.LogError(ex, "Failed to parse Yandex Translate response JSON");
                return TranslationResult.Failure(text, "Получен некорректный ответ от сервиса перевода.", normalizedTarget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Yandex translation");
                return TranslationResult.Failure(text, $"Ошибка перевода: {ex.Message}", normalizedTarget);
            }
        }

        if (lastException != null)
        {
            return TranslationResult.Failure(text, "Отсутствует подключение к интернету или сервис перевода недоступен.", normalizedTarget);
        }

        return TranslationResult.Failure(text, "Не удалось выполнить перевод.", normalizedTarget);
    }

    public static TranslationResult ParseYandexResponse(string originalText, string json, string targetLanguage)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Invalid root element in Yandex translation response");
        }

        if (root.TryGetProperty("code", out var codeElement) && codeElement.GetInt32() != 200)
        {
            var code = codeElement.GetInt32();
            var message = root.TryGetProperty("message", out var msgElem) ? msgElem.GetString() : null;
            return TranslationResult.Failure(originalText, $"Ошибка Yandex Translate (код {code}): {message}", targetLanguage);
        }

        if (!root.TryGetProperty("text", out var textElement))
        {
            throw new JsonException("Missing text property in Yandex translation response");
        }

        var translatedBuilder = new StringBuilder();

        if (textElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in textElement.EnumerateArray())
            {
                var line = item.GetString();
                if (!string.IsNullOrEmpty(line))
                {
                    if (translatedBuilder.Length > 0)
                    {
                        translatedBuilder.AppendLine();
                    }
                    translatedBuilder.Append(line);
                }
            }
        }
        else if (textElement.ValueKind == JsonValueKind.String)
        {
            translatedBuilder.Append(textElement.GetString());
        }

        var translatedText = translatedBuilder.ToString();
        if (string.IsNullOrEmpty(translatedText))
        {
            translatedText = originalText;
        }

        string? detectedLanguage = null;
        if (root.TryGetProperty("lang", out var langElement) && langElement.ValueKind == JsonValueKind.String)
        {
            var langStr = langElement.GetString();
            if (!string.IsNullOrEmpty(langStr))
            {
                var dashIndex = langStr.IndexOf('-');
                detectedLanguage = dashIndex > 0 ? langStr.Substring(0, dashIndex) : langStr;
            }
        }

        if (detectedLanguage != null && detectedLanguage.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            detectedLanguage = "zh-CN";
        }

        return TranslationResult.Success(originalText, translatedText, detectedLanguage, targetLanguage);
    }

    private static string NormalizeLangCode(string? langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode)) return string.Empty;
        var trimmed = langCode.Trim();
        if (trimmed.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh";
        }
        return trimmed.ToLowerInvariant();
    }
}
