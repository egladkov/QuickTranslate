using System.Text.Json;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class GoogleTranslateResponseParserTests
{
    [Fact]
    public void Parse_SingleSentence_ExtractsTranslationAndDetectedLanguage()
    {
        // Google Translate standard format:
        // [[["Привет, мир!","Hello, world!",null,null,10]],null,"en"]
        var json = "[[[\"Привет, мир!\",\"Hello, world!\",null,null,10]],null,\"en\"]";

        var result = GoogleTranslateService.ParseGoogleResponse("Hello, world!", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Привет, мир!", result.TranslatedText);
        Assert.Equal("Hello, world!", result.OriginalText);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Equal("ru", result.TargetLanguage);
    }

    [Fact]
    public void Parse_MultipleSentences_ConcatenatesAllSegments()
    {
        // Multiple segments in sentences array
        var json = "[[[\"Первое предложение. \",\"First sentence. \",null,null,1],[\"Второе предложение.\",\"Second sentence.\",null,null,1]],null,\"en\"]";

        var result = GoogleTranslateService.ParseGoogleResponse("First sentence. Second sentence.", json, "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Первое предложение. Второе предложение.", result.TranslatedText);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{ \"error\": \"bad request\" }";

        Assert.Throws<JsonException>(() =>
            GoogleTranslateService.ParseGoogleResponse("Test", invalidJson, "ru"));
    }

    [Fact]
    public void Parse_EmptyArray_ThrowsJsonException()
    {
        var emptyJson = "[]";

        Assert.Throws<JsonException>(() =>
            GoogleTranslateService.ParseGoogleResponse("Test", emptyJson, "ru"));
    }
}
