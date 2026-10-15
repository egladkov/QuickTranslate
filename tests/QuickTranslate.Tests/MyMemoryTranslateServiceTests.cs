using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class MyMemoryTranslateServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task TranslateAsync_EmptyOrWhitespace_ReturnsFailureWithoutNetworkCall(string input)
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings());

        var mockHandler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called"));
        var httpClient = new HttpClient(mockHandler);
        var service = new MyMemoryTranslateService(httpClient, settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var result = await service.TranslateAsync(input, "auto", "ru");

        Assert.False(result.IsSuccess);
        Assert.Contains("отсутствует", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_ValidResponse_ReturnsSuccessResult()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings { RequestTimeoutSeconds = 5 });

        var responseJson = "{\"responseData\":{\"translatedText\":\"Привет\",\"match\":1,\"detectedLanguage\":\"en\"},\"responseStatus\":200}";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(mockHandler);
        var service = new MyMemoryTranslateService(httpClient, settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var result = await service.TranslateAsync("Hello", "auto", "ru");

        Assert.True(result.IsSuccess);
        Assert.Equal("Привет", result.TranslatedText);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Equal("ru", result.TargetLanguage);
    }

    [Fact]
    public async Task TranslateAsync_DailyBandwidthExceeded_ReturnsBandwidthError()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings { RequestTimeoutSeconds = 5 });

        var responseJson = "{\"responseData\":{\"translatedText\":\"MYMEMORY WARNING: YOU USED ALL YOUR AVAILABLE FREE BANDWIDTH\"},\"responseStatus\":403}";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(mockHandler);
        var service = new MyMemoryTranslateService(httpClient, settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var result = await service.TranslateAsync("Hello", "auto", "ru");

        Assert.False(result.IsSuccess);
        Assert.Contains("лимит бесплатных запросов", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_RateLimit429_ReturnsRateLimitError()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings { RequestTimeoutSeconds = 5 });

        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage((HttpStatusCode)429));
        var httpClient = new HttpClient(mockHandler);
        var service = new MyMemoryTranslateService(httpClient, settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var result = await service.TranslateAsync("Hello", "auto", "ru");

        Assert.False(result.IsSuccess);
        Assert.Contains("лимит запросов", result.ErrorMessage);
    }
}
