using QuickTranslate.Models;

namespace QuickTranslate.Services;

public interface ITranslationService
{
    TranslationProvider Provider { get; }

    Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}
