using System.Windows.Input;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;
using Xunit;

namespace QuickTranslate.Tests;

public class SettingsViewModelTests
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IStartupService _startupService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _hotkeyService = Substitute.For<IHotkeyService>();
        _startupService = Substitute.For<IStartupService>();

        _settingsService.CurrentSettings.Returns(new AppSettings
        {
            Hotkey = new HotkeyConfig { IsControl = true, IsShift = true, Key = Key.T },
            SourceLanguage = "auto",
            TargetLanguage = "ru",
            StartWithWindows = false
        });

        _startupService.IsAutoStartEnabled().Returns(false);

        _viewModel = new SettingsViewModel(
            _settingsService,
            _hotkeyService,
            _startupService,
            NullLogger<SettingsViewModel>.Instance);
    }

    [Fact]
    public void LoadCurrentSettings_PopulatesProperties()
    {
        Assert.True(_viewModel.IsControl);
        Assert.True(_viewModel.IsShift);
        Assert.False(_viewModel.IsAlt);
        Assert.False(_viewModel.IsWin);
        Assert.Equal(Key.T, _viewModel.SelectedKey);
        Assert.Equal("Ctrl + Shift + T", _viewModel.HotkeyPreviewText);
        Assert.Equal("auto", _viewModel.SelectedSourceLanguage);
        Assert.Equal("ru", _viewModel.SelectedTargetLanguage);
        Assert.Equal(TranslationProvider.Google, _viewModel.SelectedProvider);
        Assert.Equal(3, _viewModel.AvailableProviders.Count);
        Assert.Equal(AppTheme.System, _viewModel.SelectedTheme);
        Assert.Equal(3, _viewModel.AvailableThemes.Count);
        Assert.Equal(0, _viewModel.AutoCloseSeconds);
        Assert.True(_viewModel.CloseOnClickOutside);
        Assert.Equal(5, _viewModel.AvailableAutoCloseOptions.Count);
        Assert.Contains(_viewModel.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "Выключено");
        Assert.Contains(_viewModel.AvailableAutoCloseOptions, o => o.Seconds == 5 && o.DisplayName == "5 секунд");
        Assert.Contains(_viewModel.AvailableAutoCloseOptions, o => o.Seconds == 10 && o.DisplayName == "10 секунд");
        Assert.Contains(_viewModel.AvailableAutoCloseOptions, o => o.Seconds == 15 && o.DisplayName == "15 секунд");
    }

    [Fact]
    public async Task SaveAsync_WithoutModifiers_ShowsValidationError()
    {
        _viewModel.IsControl = false;
        _viewModel.IsShift = false;
        _viewModel.IsAlt = false;
        _viewModel.IsWin = false;

        await _viewModel.SaveAsync();

        Assert.True(_viewModel.HasStatusError);
        Assert.Contains("модификатор", _viewModel.StatusMessage);
        _hotkeyService.DidNotReceiveWithAnyArgs().Register(default!, out _);
    }

    [Fact]
    public async Task SaveAsync_WhenHotkeyConflict_SetsErrorState()
    {
        _hotkeyService.Register(Arg.Any<HotkeyConfig>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "Сочетание клавиш занято.";
                return false;
            });

        await _viewModel.SaveAsync();

        Assert.True(_viewModel.HasStatusError);
        Assert.Contains("занято", _viewModel.StatusMessage);
        await _settingsService.DidNotReceiveWithAnyArgs().SaveSettingsAsync(default!);
    }

    [Fact]
    public async Task SaveAsync_WhenValid_RegistersHotkeyAndSavesSettings()
    {
        _hotkeyService.Register(Arg.Any<HotkeyConfig>(), out Arg.Any<string?>())
            .Returns(true);

        _viewModel.SelectedTargetLanguage = "de";
        _viewModel.SelectedProvider = TranslationProvider.Yandex;
        _viewModel.SelectedTheme = AppTheme.Dark;
        _viewModel.AutoCloseSeconds = 15;
        _viewModel.CloseOnClickOutside = false;

        await _viewModel.SaveAsync();

        Assert.False(_viewModel.HasStatusError);
        Assert.Contains("успешно сохранены", _viewModel.StatusMessage);
        await _settingsService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s =>
            s.TargetLanguage == "de" && s.Provider == TranslationProvider.Yandex && s.Theme == AppTheme.Dark && s.AutoCloseSeconds == 15 && !s.CloseOnClickOutside));
    }

    [Fact]
    public void ResetDefaults_RestoresDefaults()
    {
        _viewModel.SelectedKey = Key.F12;
        _viewModel.SelectedTargetLanguage = "ja";
        _viewModel.SelectedProvider = TranslationProvider.MyMemory;
        _viewModel.SelectedTheme = AppTheme.Dark;
        _viewModel.AutoCloseSeconds = 15;
        _viewModel.CloseOnClickOutside = false;

        _viewModel.ResetDefaults();

        Assert.Equal(Key.T, _viewModel.SelectedKey);
        Assert.Equal("ru", _viewModel.SelectedTargetLanguage);
        Assert.Equal(TranslationProvider.Google, _viewModel.SelectedProvider);
        Assert.Equal("Ctrl + Shift + T", _viewModel.HotkeyPreviewText);
        Assert.Equal(AppTheme.System, _viewModel.SelectedTheme);
        Assert.Equal(0, _viewModel.AutoCloseSeconds);
        Assert.True(_viewModel.CloseOnClickOutside);
    }

    [Fact]
    public void AuthorInfo_PropertiesAndCommand_AreConfiguredProperly()
    {
        Assert.Equal("Евгений Гладков", _viewModel.AuthorName);
        Assert.Equal("https://github.com/egladkov/", _viewModel.GitHubUrl);

        string? openedUrl = null;
        var vmWithCustomLauncher = new SettingsViewModel(
            _settingsService,
            _hotkeyService,
            _startupService,
            NullLogger<SettingsViewModel>.Instance,
            null,
            url => openedUrl = url);

        vmWithCustomLauncher.OpenGitHubCommand.Execute(null);

        Assert.Equal("https://github.com/egladkov/", openedUrl);
    }

    [Fact]
    public void SelectedAppLanguage_WhenChanged_UpdatesListsDynamically()
    {
        var locService = new LocalizationService(_settingsService, NullLogger<LocalizationService>.Instance);
        var vm = new SettingsViewModel(
            _settingsService,
            _hotkeyService,
            _startupService,
            NullLogger<SettingsViewModel>.Instance,
            null,
            null,
            locService);

        // Initially system / ru
        vm.SelectedAppLanguage = "en";

        Assert.Equal("en", locService.EffectiveLanguage);
        Assert.Contains(vm.AvailableThemes, t => t.DisplayName == "Dark");
        Assert.Contains(vm.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "Disabled");
        Assert.Contains(vm.AvailableSourceLanguages, l => l.Code == "en" && l.Name == "English");

        // Switch to Spanish
        vm.SelectedAppLanguage = "es";
        Assert.Equal("es", locService.EffectiveLanguage);
        Assert.Contains(vm.AvailableThemes, t => t.DisplayName == "Oscura");
        Assert.Contains(vm.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "Desactivado");
        Assert.Contains(vm.AvailableSourceLanguages, l => l.Code == "es" && l.Name == "Español");

        // Switch to German
        vm.SelectedAppLanguage = "de";
        Assert.Equal("de", locService.EffectiveLanguage);
        Assert.Contains(vm.AvailableThemes, t => t.DisplayName == "Dunkel");
        Assert.Contains(vm.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "Aus");

        // Switch to French
        vm.SelectedAppLanguage = "fr";
        Assert.Equal("fr", locService.EffectiveLanguage);
        Assert.Contains(vm.AvailableThemes, t => t.DisplayName == "Sombre");
        Assert.Contains(vm.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "Désactivé");

        // Switch to Chinese
        vm.SelectedAppLanguage = "zh-CN";
        Assert.Equal("zh-CN", locService.EffectiveLanguage);
        Assert.Contains(vm.AvailableThemes, t => t.DisplayName == "深色");
        Assert.Contains(vm.AvailableAutoCloseOptions, o => o.Seconds == 0 && o.DisplayName == "关闭");
    }

    [Fact]
    public async Task SaveAsync_WithSelectedAppLanguage_SavesLanguageToSettings()
    {
        _hotkeyService.Register(Arg.Any<HotkeyConfig>(), out Arg.Any<string?>()).Returns(true);

        _viewModel.SelectedAppLanguage = "fr";
        await _viewModel.SaveAsync();

        await _settingsService.Received(1).SaveSettingsAsync(Arg.Is<AppSettings>(s => s.AppLanguage == "fr"));
    }

    [Fact]
    public void ResetDefaults_ResetsAppLanguageToSystem()
    {
        _viewModel.SelectedAppLanguage = "es";
        _viewModel.ResetDefaults();

        Assert.Equal("system", _viewModel.SelectedAppLanguage);
    }

    [Fact]
    public void SelectedAppLanguage_WhenChanged_PreservesAllSelectedOptionsAndListReferences()
    {
        var locService = new LocalizationService(_settingsService, NullLogger<LocalizationService>.Instance);
        var vm = new SettingsViewModel(
            _settingsService,
            _hotkeyService,
            _startupService,
            NullLogger<SettingsViewModel>.Instance,
            null,
            null,
            locService);

        // Configure custom options
        vm.SelectedProvider = TranslationProvider.Yandex;
        vm.SelectedTheme = AppTheme.Dark;
        vm.SelectedSourceLanguage = "de";
        vm.SelectedTargetLanguage = "fr";
        vm.AutoCloseSeconds = 15;

        var providersRef = vm.AvailableProviders;
        var themesRef = vm.AvailableThemes;
        var autoCloseRef = vm.AvailableAutoCloseOptions;
        var sourceLangRef = vm.AvailableSourceLanguages;
        var targetLangRef = vm.AvailableTargetLanguages;

        // Switch language to English
        vm.SelectedAppLanguage = "en";

        // Assert list references are not replaced (critical for WPF ComboBox stability)
        Assert.Same(providersRef, vm.AvailableProviders);
        Assert.Same(themesRef, vm.AvailableThemes);
        Assert.Same(autoCloseRef, vm.AvailableAutoCloseOptions);
        Assert.Same(sourceLangRef, vm.AvailableSourceLanguages);
        Assert.Same(targetLangRef, vm.AvailableTargetLanguages);

        // Assert selected values remain intact
        Assert.Equal(TranslationProvider.Yandex, vm.SelectedProvider);
        Assert.Equal(AppTheme.Dark, vm.SelectedTheme);
        Assert.Equal("de", vm.SelectedSourceLanguage);
        Assert.Equal("fr", vm.SelectedTargetLanguage);
        Assert.Equal(15, vm.AutoCloseSeconds);

        // Switch language to Spanish
        vm.SelectedAppLanguage = "es";

        Assert.Equal(TranslationProvider.Yandex, vm.SelectedProvider);
        Assert.Equal(AppTheme.Dark, vm.SelectedTheme);
        Assert.Equal("de", vm.SelectedSourceLanguage);
        Assert.Equal("fr", vm.SelectedTargetLanguage);
        Assert.Equal(15, vm.AutoCloseSeconds);
    }

    [Fact]
    public void LanguageProperties_IgnoreNullOrEmptyValues()
    {
        _viewModel.SelectedSourceLanguage = "en";
        _viewModel.SelectedTargetLanguage = "ru";

        _viewModel.SelectedSourceLanguage = null!;
        Assert.Equal("en", _viewModel.SelectedSourceLanguage);

        _viewModel.SelectedSourceLanguage = "   ";
        Assert.Equal("en", _viewModel.SelectedSourceLanguage);

        _viewModel.SelectedTargetLanguage = null!;
        Assert.Equal("ru", _viewModel.SelectedTargetLanguage);

        _viewModel.SelectedTargetLanguage = "";
        Assert.Equal("ru", _viewModel.SelectedTargetLanguage);
    }
}
