using SpecialGuide.Core.Services;
using SpecialGuide.Core.Models;
using System.Threading.Tasks;
using Xunit;

namespace SpecialGuide.Tests
{
public class SuggestionServiceTests
{
    [Fact]
    public async Task Truncates_Long_Suggestions()
    {
        var capture = new FakeCaptureService();
        var openai = new FakeOpenAIService();
        var settings = new SettingsService(new Settings());
        var service = new SuggestionService(capture, openai, settings);
        var result = await service.GetSuggestionsAsync("app");
        Assert.All(result, s => Assert.True(s.Length <= SuggestionService.DefaultMaxSuggestionLength));
    }

    [Fact]
    public async Task Respects_Custom_Limit()
    {
        var capture = new FakeCaptureService();
        var openai = new FakeOpenAIService();
        var settings = new SettingsService(new Settings { MaxSuggestionLength = 10 });
        var service = new SuggestionService(capture, openai, settings);
        var result = await service.GetSuggestionsAsync("app");
        Assert.All(result, s => Assert.True(s.Length <= 10));
    }

    private class FakeCaptureService : CaptureService
    {
        public FakeCaptureService() : base(new SettingsService(new Settings())) { }
        public override byte[] CaptureScreen() => Array.Empty<byte>();
    }

    private class FakeOpenAIService : OpenAIService
    {
        public FakeOpenAIService() : base(new HttpClient(), new SettingsService(new Settings()), new LoggingService()) { }
        public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName)
            => Task.FromResult(new SuggestionResult(new[] { new string('a', 100) }, null));
    }
}

}
