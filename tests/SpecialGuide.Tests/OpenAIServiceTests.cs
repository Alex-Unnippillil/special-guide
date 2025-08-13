using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests
{
    public class OpenAIServiceTests
    {
        [Fact]
        public async Task Deserializes_StringArray_Content()
        {
            var handler = new StaticHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"[\\\"one\\\",\\\"two\\\"]\"}}]}")
            });
            var http = new HttpClient(handler);
        var service = new OpenAIService(http, new SettingsService(new Settings()), NullLogger<OpenAIService>.Instance);
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
            Assert.Equal(new[] { "one", "two" }, result);
            Assert.Null(service.LastError);
        }

        [Fact]
        public async Task Returns_Empty_And_Error_On_Invalid_Json()
        {
            var handler = new StaticHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"not json\"}}]}")
            });
            var http = new HttpClient(handler);
        var service = new OpenAIService(http, new SettingsService(new Settings()), NullLogger<OpenAIService>.Instance);
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
            Assert.Empty(result);
            Assert.NotNull(service.LastError);
        }

        [Fact]
        public async Task Retries_On_Transient_Error()
        {
            var success = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"[\\\"one\\\",\\\"two\\\"]\"}}]}")
            };
            var failure = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var handler = new SequenceHandler(failure, success);
            var http = new HttpClient(handler);
        var service = new OpenAIService(http, new SettingsService(new Settings()), NullLogger<OpenAIService>.Instance);
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
            Assert.Equal(new[] { "one", "two" }, result);
            Assert.Equal(2, handler.Calls);
            Assert.Null(service.LastError);
        }

        [Fact]
        public async Task Persistent_Failure_Returns_Empty()
        {
            var failure = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var handler = new SequenceHandler(failure, failure, failure);
            var http = new HttpClient(handler);
        var service = new OpenAIService(http, new SettingsService(new Settings()), NullLogger<OpenAIService>.Instance);
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app");
            Assert.Empty(result);
        Assert.NotNull(service.LastError);
        Assert.True(handler.Calls >= 2);
    }

        private class StaticHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;
            public StaticHandler(HttpResponseMessage response) => _response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_response);
        }

        private class SequenceHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;
            public int Calls { get; private set; }
            public SequenceHandler(params HttpResponseMessage[] responses) => _responses = new Queue<HttpResponseMessage>(responses);
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                if (_responses.Count > 0) return Task.FromResult(_responses.Dequeue());
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }
        }
    }
}

