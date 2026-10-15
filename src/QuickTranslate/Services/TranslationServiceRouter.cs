using Microsoft.Extensions.Logging;
using QuickTranslate.Models;

namespace QuickTranslate.Services;

public class TranslationServiceRouter : ITranslationService
{
    private readonly GoogleTranslateService _googleService;
    private readonly YandexTranslateService _yandexService;
    private readonly MyMemoryTranslateService _myMemoryService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TranslationServiceRouter> _logger;

    public TranslationProvider Provider => _settingsService.CurrentSettings?.Provider ?? TranslationProvider.Google;

    public TranslationServiceRouter(
        GoogleTranslateService googleService,
        YandexTranslateService yandexService,
        MyMemoryTranslateService myMemoryService,
        ISettingsService settingsService,
        ILogger<TranslationServiceRouter> logger)
    {
        _googleService = googleService;
        _yandexService = yandexService;
        _myMemoryService = myMemoryService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var provider = Provider;
        _logger.LogInformation("Routing translation to provider: {Provider}", provider);

        ITranslationService targetService = provider switch
        {
            TranslationProvider.Google => _googleService,
            TranslationProvider.Yandex => _yandexService,
            TranslationProvider.MyMemory => _myMemoryService,
            _ => _googleService
        };

        return targetService.TranslateAsync(text, sourceLanguage, targetLanguage, cancellationToken);
    }
}
