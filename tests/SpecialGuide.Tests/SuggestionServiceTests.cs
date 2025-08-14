using System;
using System.Threading;
using System.Threading.Tasks;
using SpecialGuide.Core.Services;
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
        Assert.All(result.Suggestions, s => Assert.True(s.Length <= SuggestionService.DefaultMaxSuggestionLength));
    }

    [Fact]
    public async Task Respects_Custom_Limit()
    {
        var capture = new FakeCaptureService();
        var openai = new FakeOpenAIService();
        var settings = new SettingsService(new Settings { MaxSuggestionLength = 10 });
        var service = new SuggestionService(capture, openai, settings);
        var result = await service.GetSuggestionsAsync("app");
        Assert.All(result.Suggestions, s => Assert.True(s.Length <= 10));
    }

    [Fact]
    public async Task Returns_Canceled_On_Cancellation()
    {
        var capture = new FakeCaptureService();
        var openai = new CancelingOpenAIService();
        var settings = new SettingsService(new Settings());
        var service = new SuggestionService(capture, openai, settings);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = await service.GetSuggestionsAsync("app", cts.Token);
        Assert.NotNull(result.Error);
        Assert.Equal("Canceled", result.Error!.Message);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task Propagates_Error_From_OpenAI()
    {
        var capture = new FakeCaptureService();
        var openai = new ErrorOpenAIService();
        var settings = new SettingsService(new Settings());
        var service = new SuggestionService(capture, openai, settings);
        var result = await service.GetSuggestionsAsync("app");
        Assert.Equal("boom", result.Error?.Message);
    }

    private class FakeCaptureService : CaptureService
    {
        public override byte[] CaptureScreen() => Array.Empty<byte>();
    }

    private class FakeOpenAIService : OpenAIService
    {
        public FakeOpenAIService() : base(new HttpClient(), new SettingsService(new Settings()), new LoggingService()) { }
        public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
            => Task.FromResult(new SuggestionResult(new[] { new string('a', 100) }, null));
    }

    private class CancelingOpenAIService : OpenAIService
    {
        public CancelingOpenAIService() : base(new HttpClient(), new SettingsService(new Settings()), new LoggingService()) { }
        public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new SuggestionResult(Array.Empty<string>(), null));
        }
    }

    private class ErrorOpenAIService : OpenAIService
    {
        public ErrorOpenAIService() : base(new HttpClient(), new SettingsService(new Settings()), new LoggingService()) { }
        public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
            => Task.FromResult(new SuggestionResult(Array.Empty<string>(), new OpenAIError(null, "boom")));
    }
}

}

namespace SpecialGuide.Core.Services
{
    public class SettingsService
    {
        public Settings Settings { get; }
        public string ApiKey => Settings.ApiKey;
        public SettingsService(Settings settings) => Settings = settings;
    }

    public class Settings
    {
        public int MaxSuggestionLength { get; set; } = SuggestionService.DefaultMaxSuggestionLength;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class CaptureService
    {
        public virtual byte[] CaptureScreen() => Array.Empty<byte>();

    }
}
