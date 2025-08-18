using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class SuggestionHistoryServiceTests
{
    [Fact]
    public void Keeps_Only_Last_N_Sets()
    {
        var history = new SuggestionHistoryService(2);
        history.Add(new[] { "a" });
        history.Add(new[] { "b" });
        history.Add(new[] { "c" });
        var all = history.GetHistory();
        Assert.Equal(2, all.Count);
        Assert.Equal(new[] { "c" }, all[0]);
        Assert.Equal(new[] { "b" }, all[1]);
    }

    [Fact]
    public async Task SuggestionService_Appends_To_History()
    {
        var capture = new FakeCapture();
        var openai = new FakeOpenAI();
        var settings = new SettingsService(new Settings());
        var history = new SuggestionHistoryService();
        var service = new SuggestionService(capture, openai, settings, history);
        var result = await service.GetSuggestionsAsync("app", CancellationToken.None);
        Assert.Single(history.GetHistory());
        Assert.Equal(result.Suggestions, history.GetHistory()[0]);
    }

    private class FakeCapture : CaptureService
    {
        public FakeCapture() : base(new SettingsService(new Settings())) { }
        public override byte[] CaptureScreen() => Array.Empty<byte>();
    }

    private class FakeOpenAI : OpenAIService
    {
        public FakeOpenAI() : base(new HttpClient(), new SettingsService(new Settings()), NullLogger<OpenAIService>.Instance) { }
        public override Task<SuggestionResult> GenerateSuggestionsAsync(byte[] image, string appName, CancellationToken cancellationToken = default)
            => Task.FromResult(new SuggestionResult(new[] { "one", "two" }, null));
    }
}
