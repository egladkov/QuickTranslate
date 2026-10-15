namespace QuickTranslate.Models;

public class TranslationResult
{
    public bool IsSuccess { get; init; }
    public string? OriginalText { get; init; }
    public string? TranslatedText { get; init; }
    public string? DetectedSourceLanguage { get; init; }
    public string? TargetLanguage { get; init; }
    public string? ErrorMessage { get; init; }

    public static TranslationResult Success(
        string originalText,
        string translatedText,
        string? detectedSourceLanguage,
        string targetLanguage)
    {
        return new TranslationResult
        {
            IsSuccess = true,
            OriginalText = originalText,
            TranslatedText = translatedText,
            DetectedSourceLanguage = detectedSourceLanguage,
            TargetLanguage = targetLanguage
        };
    }

    public static TranslationResult Failure(string originalText, string errorMessage, string? targetLanguage = null)
    {
        return new TranslationResult
        {
            IsSuccess = false,
            OriginalText = originalText,
            ErrorMessage = errorMessage,
            TargetLanguage = targetLanguage
        };
    }
}
