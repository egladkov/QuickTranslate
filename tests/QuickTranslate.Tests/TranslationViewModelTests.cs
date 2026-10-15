using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;
using Xunit;

namespace QuickTranslate.Tests;

public class TranslationViewModelTests
{
    private readonly ITranslationService _translationService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly TranslationViewModel _viewModel;

    public TranslationViewModelTests()
    {
        _translationService = Substitute.For<ITranslationService>();
        _clipboardService = Substitute.For<IClipboardService>();
        _settingsService = Substitute.For<ISettingsService>();

        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            SourceLanguage = "auto",
            TargetLanguage = "ru"
        });

        _viewModel = new TranslationViewModel(
            _translationService,
            _clipboardService,
            _settingsService,
            NullLogger<TranslationViewModel>.Instance);
    }

    [Fact]
    public async Task StartTranslationAsync_OnSuccess_UpdatesStateCorrectly()
    {
        _translationService.TranslateAsync(
            "Hello world",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Success("Hello world", "Привет мир", "en", "ru"));

        await _viewModel.StartTranslationAsync("Hello world");

        Assert.Equal("Hello world", _viewModel.OriginalText);
        Assert.Equal("Привет мир", _viewModel.TranslatedText);
        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.HasError);
        Assert.Contains("Английский", _viewModel.SourceLanguageDisplay);
    }

    [Fact]
    public async Task StartTranslationAsync_OnFailure_SetsErrorState()
    {
        _translationService.TranslateAsync(
            "Test",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Failure("Test", "Сетевая ошибка", "ru"));

        await _viewModel.StartTranslationAsync("Test");

        Assert.True(_viewModel.HasError);
        Assert.Equal("Сетевая ошибка", _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsLoading);
        Assert.Empty(_viewModel.TranslatedText);
    }

    [Fact]
    public async Task CopyTranslationCommand_WhenTranslatedTextAvailable_SetsClipboardAndShowsFeedback()
    {
        _translationService.TranslateAsync(
            "Hello",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Success("Hello", "Привет", "en", "ru"));

        await _viewModel.StartTranslationAsync("Hello");

        Assert.False(_viewModel.IsCopied);

        _viewModel.CopyTranslationCommand.Execute(null);

        await _clipboardService.Received(1).SetTextAsync("Привет");
        Assert.True(_viewModel.IsCopied);
    }

    [Fact]
    public async Task StartTranslationAsync_WhenFromSelectionTrue_DisablesEditingAndHidesTranslateButton()
    {
        _translationService.TranslateAsync(
            "Selected text",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Success("Selected text", "Выделенный текст", "en", "ru"));

        await _viewModel.StartTranslationAsync("Selected text", isFromSelection: true);

        Assert.True(_viewModel.IsSourceReadOnly);
        Assert.False(_viewModel.IsTranslateButtonVisible);
    }

    [Fact]
    public async Task StartTranslationAsync_WhenFromSelectionFalse_EnablesEditingAndShowsTranslateButton()
    {
        _translationService.TranslateAsync(
            "Manual text",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Success("Manual text", "Ручной текст", "en", "ru"));

        await _viewModel.StartTranslationAsync("Manual text", isFromSelection: false);

        Assert.False(_viewModel.IsSourceReadOnly);
        Assert.True(_viewModel.IsTranslateButtonVisible);
    }

    [Fact]
    public void PrepareForManualInput_ResetsReadOnlyAndShowsTranslateButton()
    {
        _viewModel.IsSourceReadOnly = true;
        _viewModel.IsTranslateButtonVisible = false;
        _viewModel.OriginalText = "Some old text";
        _viewModel.TranslatedText = "Старый перевод";

        _viewModel.PrepareForManualInput();

        Assert.False(_viewModel.IsSourceReadOnly);
        Assert.True(_viewModel.IsTranslateButtonVisible);
        Assert.Empty(_viewModel.OriginalText);
        Assert.Empty(_viewModel.TranslatedText);
    }

    [Fact]
    public void ManualTranslateCommand_WhenSourceReadOnly_DoesNotTriggerTranslation()
    {
        _viewModel.OriginalText = "Test";
        _viewModel.IsSourceReadOnly = true;

        _viewModel.ManualTranslateCommand.Execute(null);

        _translationService.DidNotReceiveWithAnyArgs().TranslateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpdateCloseHint_WhenAutoCloseZero_ShowsEscOnlyOrEmpty()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = true,
            AutoCloseSeconds = 0
        });

        _viewModel.UpdateCloseHint();
        Assert.Equal("Esc для закрытия", _viewModel.CloseHintText);

        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = false,
            AutoCloseSeconds = 0
        });

        _viewModel.UpdateCloseHint();
        Assert.Empty(_viewModel.CloseHintText);
    }

    [Fact]
    public void UpdateCloseHint_WhenAutoCloseActive_ShowsCountdownAndEsc()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = true,
            AutoCloseSeconds = 10
        });

        _viewModel.IsSourceReadOnly = true;
        _viewModel.UpdateCloseHint(8, isPaused: false);
        Assert.Equal("Esc для закрытия • Закроется через 8 сек", _viewModel.CloseHintText);
    }

    [Fact]
    public void UpdateCloseHint_WhenPaused_ShowsPausedMessage()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = true,
            AutoCloseSeconds = 5
        });

        _viewModel.IsSourceReadOnly = true;
        _viewModel.UpdateCloseHint(3, isPaused: true);
        Assert.Equal("Esc для закрытия • Таймер на паузе (3 сек)", _viewModel.CloseHintText);
    }

    [Fact]
    public void UpdateCloseHint_WhenLoading_HidesTimerHint()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = true,
            AutoCloseSeconds = 15
        });

        _viewModel.IsSourceReadOnly = true;
        _viewModel.UpdateCloseHint(15, isPaused: false, isLoading: true);
        Assert.Equal("Esc для закрытия", _viewModel.CloseHintText);
    }

    [Fact]
    public void UpdateCloseHint_WhenCloseOnEscFalse_ShowsTimerOnly()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = false,
            AutoCloseSeconds = 10
        });

        _viewModel.IsSourceReadOnly = true;
        _viewModel.UpdateCloseHint(10, isPaused: false);
        Assert.Equal("Закроется через 10 сек", _viewModel.CloseHintText);
    }

    [Fact]
    public void UpdateCloseHint_WhenManualInput_NeverShowsTimerEvenIfAutoCloseConfigured()
    {
        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = true,
            AutoCloseSeconds = 10
        });

        _viewModel.IsSourceReadOnly = false;
        _viewModel.UpdateCloseHint(10, isPaused: false);
        Assert.Equal("Esc для закрытия", _viewModel.CloseHintText);

        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            CloseOnEsc = false,
            AutoCloseSeconds = 10
        });

        _viewModel.UpdateCloseHint(10, isPaused: false);
        Assert.Empty(_viewModel.CloseHintText);
    }

    [Fact]
    public void CloseHintText_UpdatesDynamically_WhenLanguageChanges()
    {
        var locService = new LocalizationService(_settingsService, NullLogger<LocalizationService>.Instance);
        var vm = new TranslationViewModel(
            _translationService,
            _clipboardService,
            _settingsService,
            NullLogger<TranslationViewModel>.Instance,
            locService);

        locService.ApplyLanguage("en");
        Assert.Equal("Esc to close", vm.CloseHintText);

        locService.ApplyLanguage("es");
        Assert.Equal("Esc para cerrar", vm.CloseHintText);

        locService.ApplyLanguage("de");
        Assert.Equal("Esc zum Schließen", vm.CloseHintText);

        locService.ApplyLanguage("fr");
        Assert.Equal("Échap pour fermer", vm.CloseHintText);

        locService.ApplyLanguage("zh-CN");
        Assert.Equal("按 Esc 关闭", vm.CloseHintText);
    }

    [Fact]
    public void LanguageDisplay_UpdatesDynamically_WhenLanguageChanges()
    {
        var locService = new LocalizationService(_settingsService, NullLogger<LocalizationService>.Instance);
        var vm = new TranslationViewModel(
            _translationService,
            _clipboardService,
            _settingsService,
            NullLogger<TranslationViewModel>.Instance,
            locService);

        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            SourceLanguage = "en",
            TargetLanguage = "de"
        });

        locService.ApplyLanguage("en");
        Assert.Equal("English", vm.SourceLanguageDisplay);
        Assert.Equal("German", vm.TargetLanguageDisplay);

        locService.ApplyLanguage("es");
        Assert.Equal("Inglés", vm.SourceLanguageDisplay);
        Assert.Equal("Alemán", vm.TargetLanguageDisplay);

        locService.ApplyLanguage("ru");
        Assert.Equal("Английский", vm.SourceLanguageDisplay);
        Assert.Equal("Немецкий", vm.TargetLanguageDisplay);
    }

    [Fact]
    public async Task StartTranslationAsync_OnFailureWithNullErrorMessage_FallsBackToLocalizedUnknownError()
    {
        _translationService.TranslateAsync(
            "Test",
            "auto",
            "ru",
            Arg.Any<CancellationToken>())
            .Returns(TranslationResult.Failure("Test", null!, "ru"));

        await _viewModel.StartTranslationAsync("Test");

        Assert.True(_viewModel.HasError);
        Assert.Equal("Произошла ошибка при получении перевода.", _viewModel.ErrorMessage);
    }
}
