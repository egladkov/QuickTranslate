using System.Text.Json;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class MyMemoryTranslateResponseParserTests
{
    [Fact]
    public void Parse_SingleSentence_ExtractsTranslationAndDetectedLanguage()
    {
        var json = "{\"responseData\":{\"translatedText\":\"Привет мир!\",\"match\":1,\"detectedLanguage\":\"en\"},\"responseStatus\":200,\"responseDetails\":\"\"}";

        var result = MyMemoryTranslateService.ParseMyMemoryResponse("Hello world!", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Привет мир!", result.TranslatedText);
        Assert.Equal("Hello world!", result.OriginalText);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Equal("ru", result.TargetLanguage);
    }

    [Fact]
    public void Parse_HtmlEntities_DecodesProperly()
    {
        var json = "{\"responseData\":{\"translatedText\":\"Это &#39;тест&#39; &amp; пример\",\"match\":1,\"detectedLanguage\":\"en\"},\"responseStatus\":200}";

        var result = MyMemoryTranslateService.ParseMyMemoryResponse("This is 'test' & example", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Это 'тест' & пример", result.TranslatedText);
    }

    [Fact]
    public void Parse_Non200ResponseStatus_ReturnsFailureResult()
    {
        var json = "{\"responseData\":null,\"responseStatus\":403,\"responseDetails\":\"INVALID TARGET LANGUAGE\"}";

        var result = MyMemoryTranslateService.ParseMyMemoryResponse("Hello", json, "xyz");

        Assert.False(result.IsSuccess);
        Assert.Contains("INVALID TARGET LANGUAGE", result.ErrorMessage);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "invalid";

        Assert.ThrowsAny<JsonException>(() =>
            MyMemoryTranslateService.ParseMyMemoryResponse("Test", invalidJson, "ru"));
    }
}
