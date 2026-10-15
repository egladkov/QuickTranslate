using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class TranslationServiceRouterTests
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

    [Fact]
    public async Task TranslateAsync_WhenProviderIsYandex_RoutesToYandexService()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings
        {
            Provider = TranslationProvider.Yandex,
            RequestTimeoutSeconds = 5
        });

        var yandexCalled = false;
        var yandexHandler = new MockHttpMessageHandler(req =>
        {
            yandexCalled = true;
            Assert.Contains("translate.yandex.net", req.RequestUri?.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":200,\"lang\":\"en-ru\",\"text\":[\"Привет\"]}", Encoding.UTF8, "application/json")
            };
        });

        var googleHandler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Google should not be called"));
        var myMemoryHandler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("MyMemory should not be called"));

        var googleService = new GoogleTranslateService(new HttpClient(googleHandler), settingsService, NullLogger<GoogleTranslateService>.Instance);
        var yandexService = new YandexTranslateService(new HttpClient(yandexHandler), settingsService, NullLogger<YandexTranslateService>.Instance);
        var myMemoryService = new MyMemoryTranslateService(new HttpClient(myMemoryHandler), settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var router = new TranslationServiceRouter(googleService, yandexService, myMemoryService, settingsService, NullLogger<TranslationServiceRouter>.Instance);

        var result = await router.TranslateAsync("Hello", "auto", "ru");

        Assert.True(yandexCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("Привет", result.TranslatedText);
    }

    [Fact]
    public async Task TranslateAsync_WhenProviderIsMyMemory_RoutesToMyMemoryService()
    {
        var settingsService = Substitute.For<ISettingsService>();
        settingsService.CurrentSettings.Returns(new AppSettings
        {
            Provider = TranslationProvider.MyMemory,
            RequestTimeoutSeconds = 5
        });

        var myMemoryCalled = false;
        var myMemoryHandler = new MockHttpMessageHandler(req =>
        {
            myMemoryCalled = true;
            Assert.Contains("mymemory.translated.net", req.RequestUri?.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"responseData\":{\"translatedText\":\"Привет\",\"match\":1,\"detectedLanguage\":\"en\"},\"responseStatus\":200}", Encoding.UTF8, "application/json")
            };
        });

        var googleHandler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Google should not be called"));
        var yandexHandler = new MockHttpMessageHandler(_ => throw new InvalidOperationException("Yandex should not be called"));

        var googleService = new GoogleTranslateService(new HttpClient(googleHandler), settingsService, NullLogger<GoogleTranslateService>.Instance);
        var yandexService = new YandexTranslateService(new HttpClient(yandexHandler), settingsService, NullLogger<YandexTranslateService>.Instance);
        var myMemoryService = new MyMemoryTranslateService(new HttpClient(myMemoryHandler), settingsService, NullLogger<MyMemoryTranslateService>.Instance);

        var router = new TranslationServiceRouter(googleService, yandexService, myMemoryService, settingsService, NullLogger<TranslationServiceRouter>.Instance);

        var result = await router.TranslateAsync("Hello", "auto", "ru");

        Assert.True(myMemoryCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("Привет", result.TranslatedText);
    }
}
