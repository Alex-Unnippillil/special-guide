using System.Collections.Generic;
using System.Runtime.InteropServices;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;

namespace SpecialGuide.Tests;

public class ClipboardServiceTests
{
    [Fact]
    public void SetText_RetriesUntilSuccess_ThenAutoPastesOnce()
    {
        var settingsService = new SettingsService(new Settings { AutoPaste = true });
        var service = new StubClipboardService(settingsService, new Exception?[]
        {
            new ExternalException(),
            null
        });

        var result = service.SetText("text");

        Assert.True(result);
        Assert.Equal(2, service.AttemptCount);
        Assert.Equal(1, service.PasteCount);
        settingsService.Dispose();
    }

    [Fact]
    public void SetText_ThrowsAfterRetries_NoAutoPaste()
    {
        var settingsService = new SettingsService(new Settings { AutoPaste = true });
        var service = new StubClipboardService(settingsService, new Exception?[]
        {
            new ExternalException(),
            new ExternalException(),
            new ExternalException()
        });

        Assert.Throws<ClipboardWriteException>(() => service.SetText("text"));
        Assert.Equal(3, service.AttemptCount);
        Assert.Equal(0, service.PasteCount);
        settingsService.Dispose();
    }

    [Fact]
    public void SetText_DoesNotPaste_WhenAutoPasteDisabled()
    {
        var settingsService = new SettingsService(new Settings { AutoPaste = false });
        var service = new StubClipboardService(settingsService, new Exception?[] { null });

        var result = service.SetText("text");

        Assert.True(result);
        Assert.Equal(1, service.AttemptCount);
        Assert.Equal(0, service.PasteCount);
        settingsService.Dispose();
    }

    private class StubClipboardService : ClipboardService
    {
        private readonly Queue<Exception?> _results;
        public int AttemptCount { get; private set; }
        public int PasteCount { get; private set; }

        public StubClipboardService(SettingsService settings, IEnumerable<Exception?> results)
            : base(settings)
        {
            _results = new Queue<Exception?>(results);
        }

        protected override void ClipboardSetText(string text)
        {
            AttemptCount++;
            if (_results.Count > 0)
            {
                var ex = _results.Dequeue();
                if (ex != null)
                {
                    throw ex;
                }
            }
        }

        protected override void SendCtrlV()
        {
            PasteCount++;
        }
    }
}
