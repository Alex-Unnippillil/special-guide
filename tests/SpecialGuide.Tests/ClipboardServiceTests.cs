using Microsoft.Extensions.Logging;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class ClipboardServiceTests
{
    private class TestLoggingService : LoggingService
    {
        public List<string> Messages { get; } = new();

        public TestLoggingService() : base(new LoggerFactory().CreateLogger<LoggingService>()) { }

        public override void LogError(Exception ex, string message) => Messages.Add(message);
    }

    private class TestClipboardService : ClipboardService
    {
        public bool ThrowOnSet { get; set; }
        public bool ThrowOnSend { get; set; }

        public TestClipboardService(LoggingService logger) : base(logger) { }

        protected override void SetClipboardText(string text)
        {
            if (ThrowOnSet)
            {
                throw new Exception("SetClipboardText failure");
            }
        }

        protected override uint SendInputWrapper(uint nInputs, INPUT[] pInputs, int cbSize)
        {
            if (ThrowOnSend)
            {
                throw new Exception("SendInput failure");
            }
            return 0;
        }
    }

    [Fact]
    public void SetText_ClipboardFails_ReturnsFalseAndLogs()
    {
        var logger = new TestLoggingService();
        var service = new TestClipboardService(logger) { ThrowOnSet = true };

        var result = service.SetText("test");

        Assert.False(result);
        Assert.Contains("Failed to set clipboard text.", logger.Messages);
    }

    [Fact]
    public void SetText_SendInputFails_ReturnsFalseAndLogs()
    {
        var logger = new TestLoggingService();
        var service = new TestClipboardService(logger) { AutoPaste = true, ThrowOnSend = true };

        var result = service.SetText("test");

        Assert.False(result);
        Assert.Contains("Failed to send Ctrl+V.", logger.Messages);
    }
}

