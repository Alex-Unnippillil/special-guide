using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SpecialGuide.Core.Services;

namespace SpecialGuide.Tests;

public class OpenAIServiceTests
{
    private OpenAIService CreateService(HttpResponseMessage response)
    {
        var handler = new FakeHandler(response);
        var client = new HttpClient(handler);
        var factory = new FakeFactory(client);
        var logger = new LoggingService(NullLogger<LoggingService>.Instance);
        var settings = new SettingsService();
        settings.Settings.ApiKey = "test";
        return new OpenAIService(settings, factory, logger);
    }

    [Fact]
    public async Task ChatAsync_Returns_Content_On_Success()
    {
        var json = "{\"choices\":[{\"message\":{\"content\":\"hi\"}}]}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var service = CreateService(response);
        var result = await service.ChatAsync("prompt");
        Assert.Equal("hi", result);
    }

    [Fact]
    public async Task ChatAsync_Throws_On_Error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("err") };
        var service = CreateService(response);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.ChatAsync("prompt"));
    }

    [Fact]
    public async Task TranscribeAsync_Returns_Text_On_Success()
    {
        var json = "{\"text\":\"hello\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var service = CreateService(response);
        var result = await service.TranscribeAsync(Array.Empty<byte>());
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task TranscribeAsync_Throws_On_Error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("err") };
        var service = CreateService(response);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.TranscribeAsync(Array.Empty<byte>()));
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_Returns_Array_On_Success()
    {
        var json = "{\"choices\":[{\"message\":{\"content\":\"[\\\"a\\\",\\\"b\\\"]\"}}]}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        var service = CreateService(response);
        var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
        Assert.Equal(new[] { "a", "b" }, result);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_Throws_On_Error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("err") };
        var service = CreateService(response);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app"));
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FakeHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_response);
    }

    private class FakeFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}
