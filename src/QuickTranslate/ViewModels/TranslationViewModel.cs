using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickTranslate.Models;
using QuickTranslate.Services;

namespace QuickTranslate.ViewModels;

public partial class TranslationViewModel : ObservableObject, IDisposable
{
    private readonly ITranslationService _translationService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TranslationViewModel> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly Action<AppSettings> _settingsChangedHandler;
    private readonly Action<string> _languageChangedHandler;

    private CancellationTokenSource? _currentTranslationCts;
    private CancellationTokenSource? _copiedFeedbackCts;

    [ObservableProperty]
    private string _originalText = string.Empty;

    [ObservableProperty]
    private string _translatedText = string.Empty;

    [ObservableProperty]
    private string _sourceLanguageDisplay = string.Empty;

    [ObservableProperty]
    private string _targetLanguageDisplay = string.Empty;

    [ObservableProperty]
    private string _serviceProviderDisplay = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isCopied;

    [ObservableProperty]
    private bool _isSourceReadOnly;

    [ObservableProperty]
    private bool _isTranslateButtonVisible = true;

    [ObservableProperty]
    private string _closeHintText = "Esc для закрытия";

    public event Action? RequestClose;

    public TranslationViewModel(
        ITranslationService translationService,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        ILogger<TranslationViewModel> logger,
        ILocalizationService? localizationService = null)
    {
        _translationService = translationService;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _logger = logger;
        _localizationService = localizationService ?? LocalizationService.Instance;

        _settingsChangedHandler = _ =>
        {
            UpdateLanguageDisplay();
            UpdateCloseHint();
        };
        _languageChangedHandler = _ =>
        {
            UpdateLanguageDisplay();
            UpdateCloseHint();
        };

        UpdateLanguageDisplay();
        UpdateCloseHint();
        _settingsService.SettingsChanged += _settingsChangedHandler;
        _localizationService.LanguageChanged += _languageChangedHandler;
    }

    public void PrepareForManualInput()
    {
        _currentTranslationCts?.Cancel();
        _copiedFeedbackCts?.Cancel();
        OriginalText = string.Empty;
        TranslatedText = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = false;
        IsCopied = false;
        IsSourceReadOnly = false;
        IsTranslateButtonVisible = true;
        UpdateLanguageDisplay();
        UpdateCloseHint();
    }

    public void ResetCopiedState()
    {
        _copiedFeedbackCts?.Cancel();
        IsCopied = false;
    }

    public void UpdateCloseHint(int remainingSeconds = 0, bool isPaused = false, bool isLoading = false)
    {
        var settings = _settingsService.CurrentSettings;
        var closeOnEsc = settings?.CloseOnEsc ?? true;
        var autoCloseSeconds = settings?.AutoCloseSeconds ?? 0;
        var escHint = _localizationService.GetString("Translation_EscToClose");

        // Auto-close timer only runs when translating selected text (IsSourceReadOnly == true)
        if (autoCloseSeconds <= 0 || isLoading || !IsSourceReadOnly)
        {
            CloseHintText = closeOnEsc ? escHint : string.Empty;
            return;
        }

        if (remainingSeconds <= 0)
        {
            remainingSeconds = autoCloseSeconds;
        }

        var timerText = isPaused
            ? _localizationService.GetString("Translation_TimerPausedFormat", remainingSeconds)
            : _localizationService.GetString("Translation_ClosingInFormat", remainingSeconds);

        CloseHintText = closeOnEsc
            ? $"{escHint} • {timerText}"
            : timerText;
    }

    public void UpdateLanguageDisplay()
    {
        var settings = _settingsService.CurrentSettings;
        var src = settings?.SourceLanguage ?? "auto";
        var tgt = settings?.TargetLanguage ?? "ru";
        var provider = settings?.Provider ?? TranslationProvider.Google;

        var effLang = _localizationService.EffectiveLanguage;
        SourceLanguageDisplay = Language.GetDisplayName(src, effLang);
        TargetLanguageDisplay = Language.GetDisplayName(tgt, effLang);
        ServiceProviderDisplay = provider switch
        {
            TranslationProvider.Google => "Google",
            TranslationProvider.Yandex => "Yandex",
            TranslationProvider.MyMemory => "MyMemory",
            _ => provider.ToString()
        };
    }

    public async Task StartTranslationAsync(string text, bool isFromSelection = false)
    {
        IsSourceReadOnly = isFromSelection;
        IsTranslateButtonVisible = !isFromSelection;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Cancel any pending translation to prevent multiple simultaneous requests
        _currentTranslationCts?.Cancel();
        _currentTranslationCts?.Dispose();
        var cts = new CancellationTokenSource();
        _currentTranslationCts = cts;
        var cancellationToken = cts.Token;

        OriginalText = text.Trim();
        TranslatedText = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        IsLoading = true;
        IsCopied = false;
        UpdateCloseHint(isLoading: true);

        var settings = _settingsService.CurrentSettings;
        var srcLang = settings.SourceLanguage;
        var tgtLang = settings.TargetLanguage;

        UpdateLanguageDisplay();

        try
        {
            var result = await _translationService.TranslateAsync(
                OriginalText,
                srcLang,
                tgtLang,
                cancellationToken);

            if (cancellationToken.IsCancellationRequested || !ReferenceEquals(_currentTranslationCts, cts))
            {
                return;
            }

            if (result.IsSuccess)
            {
                TranslatedText = result.TranslatedText ?? string.Empty;
                if (!string.IsNullOrEmpty(result.DetectedSourceLanguage))
                {
                    var detectedName = Language.GetDisplayName(result.DetectedSourceLanguage, _localizationService.EffectiveLanguage);
                    var detectedSuffix = _localizationService.GetString("Language_Detected");
                    SourceLanguageDisplay = $"{detectedName} ({detectedSuffix})";
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? _localizationService.GetString("Translation_UnknownError");
            }
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled, ignore
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested && ReferenceEquals(_currentTranslationCts, cts))
            {
                _logger.LogError(ex, "Unhandled exception in translation view model");
                HasError = true;
                ErrorMessage = _localizationService.GetString("Common_ErrorPrefix", ex.Message);
            }
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested && ReferenceEquals(_currentTranslationCts, cts))
            {
                IsLoading = false;
                UpdateCloseHint();
            }
        }
    }

    [RelayCommand]
    public async Task ManualTranslateAsync()
    {
        if (IsSourceReadOnly)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(OriginalText))
        {
            await StartTranslationAsync(OriginalText, isFromSelection: false);
        }
    }

    [RelayCommand]
    private async Task CopyTranslationAsync()
    {
        if (string.IsNullOrEmpty(TranslatedText) || HasError)
        {
            return;
        }

        try
        {
            await _clipboardService.SetTextAsync(TranslatedText);
            _logger.LogInformation("Copied translated text to clipboard");

            // Show copied indicator
            _copiedFeedbackCts?.Cancel();
            _copiedFeedbackCts?.Dispose();
            var copyCts = new CancellationTokenSource();
            _copiedFeedbackCts = copyCts;

            IsCopied = true;

            try
            {
                await Task.Delay(1500, copyCts.Token);
                if (ReferenceEquals(_copiedFeedbackCts, copyCts))
                {
                    IsCopied = false;
                }
            }
            catch (OperationCanceledException)
            {
                // New copy action started
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy translated text to clipboard");
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= _settingsChangedHandler;
        _localizationService.LanguageChanged -= _languageChangedHandler;
        _currentTranslationCts?.Cancel();
        _currentTranslationCts?.Dispose();
        _copiedFeedbackCts?.Cancel();
        _copiedFeedbackCts?.Dispose();
    }
}
