using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class LocalizationServiceTests
{
    private readonly ISettingsService _settingsService;
    private readonly LocalizationService _localizationService;

    public LocalizationServiceTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _settingsService.CurrentSettings.Returns(new AppSettings { AppLanguage = "system" });
        _localizationService = new LocalizationService(_settingsService, NullLogger<LocalizationService>.Instance);
    }

    [Theory]
    [InlineData("ru", "ru")]
    [InlineData("en", "en")]
    [InlineData("es", "es")]
    [InlineData("de", "de")]
    [InlineData("fr", "fr")]
    [InlineData("zh-CN", "zh-CN")]
    public void ApplyLanguage_ExplicitLanguage_SetsEffectiveLanguageCorrectly(string inputCode, string expectedEffective)
    {
        string? firedWith = null;
        _localizationService.LanguageChanged += lang => firedWith = lang;

        _localizationService.ApplyLanguage(inputCode);

        Assert.Equal(inputCode, _localizationService.CurrentLanguage);
        Assert.Equal(expectedEffective, _localizationService.EffectiveLanguage);
        Assert.Equal(expectedEffective, firedWith);
    }

    [Fact]
    public void ApplyLanguage_InvalidLanguage_FallsBackToDefaultRussian()
    {
        _localizationService.ApplyLanguage("unsupported_xyz");

        Assert.Equal("unsupported_xyz", _localizationService.CurrentLanguage);
        Assert.Equal("ru", _localizationService.EffectiveLanguage);
    }

    [Theory]
    [InlineData("ru-RU", "ru")]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("es-ES", "es")]
    [InlineData("de-DE", "de")]
    [InlineData("fr-FR", "fr")]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("zh-TW", "zh-CN")]
    [InlineData("ja-JP", "en")] // Unsupported system language falls back to English
    public void ResolveSystemLanguageCode_ResolvesCorrectly(string cultureName, string expectedCode)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var resolved = LocalizationService.ResolveSystemLanguageCode(culture);

        Assert.Equal(expectedCode, resolved);
    }

    [Fact]
    public void GetString_ReturnsCorrectTranslations_AcrossAllLanguages()
    {
        _localizationService.ApplyLanguage("ru");
        Assert.Equal("Сохранить", _localizationService.GetString("Common_Save"));
        Assert.Equal("Настройки переводчика", _localizationService.GetString("Settings_HeaderTitle"));

        _localizationService.ApplyLanguage("en");
        Assert.Equal("Save", _localizationService.GetString("Common_Save"));
        Assert.Equal("Translator Settings", _localizationService.GetString("Settings_HeaderTitle"));

        _localizationService.ApplyLanguage("es");
        Assert.Equal("Guardar", _localizationService.GetString("Common_Save"));
        Assert.Equal("Configuración del traductor", _localizationService.GetString("Settings_HeaderTitle"));

        _localizationService.ApplyLanguage("de");
        Assert.Equal("Speichern", _localizationService.GetString("Common_Save"));
        Assert.Equal("Übersetzer-Einstellungen", _localizationService.GetString("Settings_HeaderTitle"));

        _localizationService.ApplyLanguage("fr");
        Assert.Equal("Enregistrer", _localizationService.GetString("Common_Save"));
        Assert.Equal("Paramètres du traducteur", _localizationService.GetString("Settings_HeaderTitle"));

        _localizationService.ApplyLanguage("zh-CN");
        Assert.Equal("保存", _localizationService.GetString("Common_Save"));
        Assert.Equal("翻译器设置", _localizationService.GetString("Settings_HeaderTitle"));
    }

    [Fact]
    public void GetString_WithFormatArguments_FormatsProperly()
    {
        _localizationService.ApplyLanguage("ru");
        Assert.Equal("Закроется через 10 сек", _localizationService.GetString("Translation_ClosingInFormat", 10));

        _localizationService.ApplyLanguage("en");
        Assert.Equal("Closes in 10 sec", _localizationService.GetString("Translation_ClosingInFormat", 10));

        _localizationService.ApplyLanguage("es");
        Assert.Equal("Se cierra en 10 seg", _localizationService.GetString("Translation_ClosingInFormat", 10));
    }

    [Fact]
    public void GetString_MissingKey_FallsBackGracefully()
    {
        _localizationService.ApplyLanguage("en");
        var result = _localizationService.GetString("NonExistent_Key_XYZ");

        Assert.Equal("NonExistent_Key_XYZ", result);
    }

    [Fact]
    public void SupportedAppLanguages_ContainsAllExpectedLanguages()
    {
        _localizationService.ApplyLanguage("en");
        var list = _localizationService.SupportedAppLanguages;

        Assert.Equal(7, list.Count);
        Assert.Contains(list, x => x.Code == "system" && x.DisplayName == "System (Default)");
        Assert.Contains(list, x => x.Code == "ru" && x.DisplayName == "Русский");
        Assert.Contains(list, x => x.Code == "en" && x.DisplayName == "English");
        Assert.Contains(list, x => x.Code == "es" && x.DisplayName == "Español");
        Assert.Contains(list, x => x.Code == "de" && x.DisplayName == "Deutsch");
        Assert.Contains(list, x => x.Code == "fr" && x.DisplayName == "Français");
        Assert.Contains(list, x => x.Code == "zh-CN" && x.DisplayName == "中文 (简体)");
    }

    [Fact]
    public void Initialize_SubscribesToSettingsChanged_AndAppliesLanguage()
    {
        _localizationService.Initialize();

        _settingsService.SettingsChanged += Raise.Event<Action<AppSettings>>(new AppSettings
        {
            AppLanguage = "de"
        });

        Assert.Equal("de", _localizationService.CurrentLanguage);
        Assert.Equal("de", _localizationService.EffectiveLanguage);
    }

    [Fact]
    public void Dispose_UnsubscribesGracefully()
    {
        _localizationService.Initialize();
        _localizationService.Dispose();

        _settingsService.SettingsChanged += Raise.Event<Action<AppSettings>>(new AppSettings
        {
            AppLanguage = "fr"
        });

        // After dispose, should not update
        Assert.Equal("system", _localizationService.CurrentLanguage);
    }

    [Fact]
    public void AllLanguages_ContainTranslationUnknownErrorKey()
    {
        foreach (var (langCode, dict) in LanguageResources.Translations)
        {
            Assert.True(dict.ContainsKey("Translation_UnknownError"), $"Language '{langCode}' is missing 'Translation_UnknownError'");
            Assert.False(string.IsNullOrWhiteSpace(dict["Translation_UnknownError"]), $"Language '{langCode}' has empty 'Translation_UnknownError'");
        }
    }

    [Fact]
    public void AllLanguages_HaveSymmetricKeySetWithDefaultLanguage()
    {
        var defaultDict = LanguageResources.Translations[LanguageResources.DefaultLanguage];
        foreach (var (langCode, dict) in LanguageResources.Translations)
        {
            if (langCode == LanguageResources.DefaultLanguage) continue;

            foreach (var key in defaultDict.Keys)
            {
                Assert.True(dict.ContainsKey(key), $"Language '{langCode}' is missing key '{key}' present in default language '{LanguageResources.DefaultLanguage}'");
            }
        }
    }

    [Fact]
    public void Language_GetDisplayName_ResolvesDirectlyAndHandlesPrefixes()
    {
        // Standard code
        Assert.Equal("English", Language.GetDisplayName("en", "en"));
        Assert.Equal("Английский", Language.GetDisplayName("en", "ru"));

        // Prefix code
        Assert.Equal("English", Language.GetDisplayName("en-US", "en"));
        Assert.Equal("Английский", Language.GetDisplayName("en-GB", "ru"));

        // Chinese alias
        Assert.Equal("Chinese (Simplified)", Language.GetDisplayName("zh", "en"));
        Assert.Equal("Китайский (упрощенный)", Language.GetDisplayName("zh", "ru"));

        // Fallback for unknown code
        Assert.Equal("xyz123", Language.GetDisplayName("xyz123", "en"));
        Assert.Equal(string.Empty, Language.GetDisplayName("", "en"));
    }
}
