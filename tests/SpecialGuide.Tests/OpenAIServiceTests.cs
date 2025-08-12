using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests
{
    public class OpenAIServiceTests
    {
        [Fact]
        public async Task Deserializes_StringArray_Content()
        {
            var handler = new FakeHandler("{\"choices\":[{\"message\":{\"content\":\"[\\\"one\\\",\\\"two\\\"]\"}}]}");
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
            Assert.Equal(new[] { "one", "two" }, result);
        }

        [Fact]
        public async Task Throws_On_Invalid_Json()
        {
            var handler = new FakeHandler("{\"choices\":[{\"message\":{\"content\":\"not json\"}}]}");
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            await Assert.ThrowsAsync<JsonException>(() => service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app"));
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _response;
            public FakeHandler(string response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var message = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_response)
                };
                return Task.FromResult(message);
            }
        }
    }
}

namespace SpecialGuide.Core.Services
{
    public class LoggingService
    {
        public void LogError(Exception ex, string message) { }
    }
}
