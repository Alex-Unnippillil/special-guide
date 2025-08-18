using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SpecialGuide.Core.Services;
using SpecialGuide.Core.Models;
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
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app", CancellationToken.None);
            Assert.Equal(new[] { "one", "two" }, result.Suggestions);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task Returns_Error_On_Invalid_Json()
        {
            var handler = new FakeHandler("{\"choices\":[{\"message\":{\"content\":\"not json\"}}]}");
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app", CancellationToken.None);
            Assert.NotNull(result.Error);
            Assert.Equal("Malformed response from OpenAI", result.Error!.Message);
            Assert.Empty(result.Suggestions);
        }

        [Fact]
        public async Task Retries_On_429_Then_Succeeds()
        {
            var responses = new[]
            {
                new HttpResponseMessage((HttpStatusCode)429),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"[]\"}}]}")
                }
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app", CancellationToken.None);
            Assert.Equal(2, handler.Calls);
            Assert.Empty(result.Suggestions);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task Gives_Up_After_Retries()
        {
            var responses = new[]
            {
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app", CancellationToken.None);
            Assert.Equal(3, handler.Calls);
            Assert.Empty(result.Suggestions);
            Assert.NotNull(result.Error);
            Assert.Equal(HttpStatusCode.InternalServerError, result.Error!.StatusCode);
        }

        [Fact]
        public async Task Gives_Up_On_RateLimit()
        {
            var responses = new[]
            {
                new HttpResponseMessage((HttpStatusCode)429),
                new HttpResponseMessage((HttpStatusCode)429),
                new HttpResponseMessage((HttpStatusCode)429)
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.GenerateSuggestionsAsync(Array.Empty<byte>(), "app", CancellationToken.None);
            Assert.Equal(3, handler.Calls);
            Assert.Empty(result.Suggestions);
            Assert.NotNull(result.Error);
            Assert.Equal((HttpStatusCode)429, result.Error!.StatusCode);
        }

        [Fact]
        public async Task ChatAsync_Retries_On_429_Then_Succeeds()
        {
            var responses = new[]
            {
                new HttpResponseMessage((HttpStatusCode)429),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"hi\"}}]}")
                }
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.ChatAsync("hello", CancellationToken.None);
            Assert.Equal(2, handler.Calls);
            Assert.Equal("hi", result);
        }

        [Fact]
        public async Task ChatAsync_Gives_Up_After_Retries()
        {
            var responses = new[]
            {
                new HttpResponseMessage(HttpStatusCode.BadGateway),
                new HttpResponseMessage(HttpStatusCode.BadGateway),
                new HttpResponseMessage(HttpStatusCode.BadGateway)
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.ChatAsync("hello", CancellationToken.None));
            Assert.Equal(3, handler.Calls);
            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        }

        [Fact]
        public async Task TranscribeAsync_Retries_On_500_Then_Succeeds()
        {
            var responses = new[]
            {
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"text\":\"ok\"}")
                }
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var result = await service.TranscribeAsync(Array.Empty<byte>(), CancellationToken.None);
            Assert.Equal(2, handler.Calls);
            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task TranscribeAsync_Gives_Up_After_Retries()
        {
            var responses = new[]
            {
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.InternalServerError),
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            };
            var handler = new SequenceHandler(responses);
            var http = new HttpClient(handler);
            var service = new OpenAIService(http, new SettingsService(new Settings()), new LoggingService());
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.TranscribeAsync(Array.Empty<byte>(), CancellationToken.None));
            Assert.Equal(3, handler.Calls);
            Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
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

        private class SequenceHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;
            public int Calls { get; private set; }
            public SequenceHandler(IEnumerable<HttpResponseMessage> responses) => _responses = new Queue<HttpResponseMessage>(responses);
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}

namespace SpecialGuide.Core.Services
{
    public class LoggingService : ILogger<OpenAIService>
    {
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        public void LogError(Exception ex, string message) { }
        private class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
    }
}
