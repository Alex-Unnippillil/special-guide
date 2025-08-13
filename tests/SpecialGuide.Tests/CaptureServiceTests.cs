using System.Drawing;
using Xunit;

namespace SpecialGuide.Tests;

public class CaptureServiceTests
{
    private class CaptureService
    {
        public byte[] CaptureScreen()
            => IsGraphicsCaptureAvailable()
                ? CaptureWithGraphicsCapture(GetActiveWindowBounds())
                : CaptureFromScreen(GetActiveWindowBounds());

        protected virtual bool IsGraphicsCaptureAvailable() => false;
        protected virtual Rectangle GetActiveWindowBounds() => Rectangle.Empty;
        protected virtual byte[] CaptureWithGraphicsCapture(Rectangle bounds) => Array.Empty<byte>();
        protected virtual byte[] CaptureFromScreen(Rectangle bounds) => Array.Empty<byte>();
    }

    private class TestCaptureService : CaptureService
    {
        private readonly bool _graphicsAvailable;
        public bool UsedGraphics { get; private set; }
        public bool UsedScreen { get; private set; }

        public TestCaptureService(bool graphicsAvailable)
        {
            _graphicsAvailable = graphicsAvailable;
        }

        protected override bool IsGraphicsCaptureAvailable() => _graphicsAvailable;
        protected override Rectangle GetActiveWindowBounds() => new Rectangle(0, 0, 1, 1);

        protected override byte[] CaptureWithGraphicsCapture(Rectangle bounds)
        {
            UsedGraphics = true;
            return Array.Empty<byte>();
        }

        protected override byte[] CaptureFromScreen(Rectangle bounds)
        {
            UsedScreen = true;
            return Array.Empty<byte>();
        }
    }

    [Fact]
    public void Uses_Graphics_Capture_When_Available()
    {
        var svc = new TestCaptureService(true);
        svc.CaptureScreen();
        Assert.True(svc.UsedGraphics);
        Assert.False(svc.UsedScreen);
    }

    [Fact]
    public void Falls_Back_When_Graphics_Capture_Not_Available()
    {
        var svc = new TestCaptureService(false);
        svc.CaptureScreen();
        Assert.False(svc.UsedGraphics);
        Assert.True(svc.UsedScreen);
    }
}
