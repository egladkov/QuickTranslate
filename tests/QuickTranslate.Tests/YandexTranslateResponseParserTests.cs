using System.Text.Json;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class YandexTranslateResponseParserTests
{
    [Fact]
    public void Parse_SingleSentence_ExtractsTranslationAndDetectedLanguage()
    {
        var json = "{\"code\":200,\"lang\":\"en-ru\",\"text\":[\"Привет, мир!\"]}";

        var result = YandexTranslateService.ParseYandexResponse("Hello, world!", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Привет, мир!", result.TranslatedText);
        Assert.Equal("Hello, world!", result.OriginalText);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Equal("ru", result.TargetLanguage);
    }

    [Fact]
    public void Parse_MultipleSentences_JoinsLinesWithNewlines()
    {
        var json = "{\"code\":200,\"lang\":\"en-ru\",\"text\":[\"Первая строка.\",\"Вторая строка.\"]}";

        var result = YandexTranslateService.ParseYandexResponse("First line.\nSecond line.", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal($"Первая строка.{Environment.NewLine}Вторая строка.", result.TranslatedText);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Fact]
    public void Parse_ChineseLanguageCode_NormalizesToZhCN()
    {
        var json = "{\"code\":200,\"lang\":\"zh-ru\",\"text\":[\"Привет\"]}";

        var result = YandexTranslateService.ParseYandexResponse("你好", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("zh-CN", result.DetectedSourceLanguage);
    }

    [Fact]
    public void Parse_LangWithoutDash_ExtractsDetectedLanguageCorrectly()
    {
        var json = "{\"code\":200,\"lang\":\"en\",\"text\":[\"Привет\"]}";

        var result = YandexTranslateService.ParseYandexResponse("Hello", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Fact]
    public void Parse_ErrorCodeInJson_ReturnsFailureResult()
    {
        var json = "{\"code\":403,\"message\":\"Blocked\"}";

        var result = YandexTranslateService.ParseYandexResponse("Hello", json, "ru");

        Assert.False(result.IsSuccess);
        Assert.Contains("403", result.ErrorMessage);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "not json";

        Assert.ThrowsAny<JsonException>(() =>
            YandexTranslateService.ParseYandexResponse("Test", invalidJson, "ru"));
    }

    [Fact]
    public void Parse_RootNotObject_ThrowsJsonException()
    {
        var arrayJson = "[]";

        Assert.Throws<JsonException>(() =>
            YandexTranslateService.ParseYandexResponse("Test", arrayJson, "ru"));
    }
}
