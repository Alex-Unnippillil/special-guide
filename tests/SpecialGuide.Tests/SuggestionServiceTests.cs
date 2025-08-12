using Microsoft.Extensions.Options;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using System.Threading.Tasks;
using Xunit;

namespace SpecialGuide.Tests;

public class SuggestionServiceTests
{
    [Fact]
    public async Task Truncates_Long_Suggestions()
    {
        var capture = new FakeCaptureService();
        var openai = new FakeOpenAIService();
        var service = new SuggestionService(capture, openai);
        var result = await service.GetSuggestionsAsync("app");
        Assert.All(result, s => Assert.True(s.Length <= 80));
    }

    private class FakeCaptureService : CaptureService
    {
        public override byte[] CaptureScreen() => Array.Empty<byte>();
    }

    private class FakeOpenAIService : OpenAIService
    {
        public FakeOpenAIService() : base(new SettingsService(new FakeOptionsMonitor<Settings>(new Settings()))) { }
        public override async Task<string[]> GenerateSuggestionsAsync(byte[] image, string appName)
        {
            await Task.CompletedTask;
            return new[] { new string('a', 100) };
        }
    }

    private class FakeOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public FakeOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string> listener) => new DummyDisposable();

        private sealed class DummyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
