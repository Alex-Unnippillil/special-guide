using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class CaptureServiceTests
{
    [Fact]
    public void ActiveWindowMode_Uses_GraphicsCapture_When_Supported()
    {
        var settings = new Settings { CaptureMode = CaptureMode.ActiveWindow };
        var service = new TestCaptureService(new SettingsService(settings), supported: true);
        service.CaptureScreen();
        Assert.True(service.ActiveWindowCalled);
        Assert.False(service.FullScreenCalled);
    }

    [Fact]
    public void ActiveWindowMode_Falls_Back_When_Not_Supported()
    {
        var settings = new Settings { CaptureMode = CaptureMode.ActiveWindow };
        var service = new TestCaptureService(new SettingsService(settings), supported: false);
        service.CaptureScreen();
        Assert.False(service.ActiveWindowCalled);
        Assert.True(service.FullScreenCalled);
    }

    [Fact]
    public void CursorRegionMode_Captures_Region()
    {
        var settings = new Settings { CaptureMode = CaptureMode.CursorRegion };
        var service = new TestCaptureService(new SettingsService(settings), supported: true);
        service.CaptureScreen();
        Assert.True(service.CursorRegionCalled);
        Assert.False(service.FullScreenCalled);
    }

    private class TestCaptureService : CaptureService
    {
        private readonly bool _supported;
        public bool ActiveWindowCalled { get; private set; }
        public bool FullScreenCalled { get; private set; }
        public bool CursorRegionCalled { get; private set; }

        public TestCaptureService(SettingsService settings, bool supported) : base(settings)
        {
            _supported = supported;
        }

        protected override bool IsGraphicsCaptureSupported() => _supported;

        protected override byte[] CaptureActiveWindow()
        {
            ActiveWindowCalled = true;
            return Array.Empty<byte>();
        }

        protected override byte[] CaptureFullScreen()
        {
            FullScreenCalled = true;
            return Array.Empty<byte>();
        }

        protected override byte[] CaptureCursorRegion(int width = 400, int height = 400)
        {
            CursorRegionCalled = true;
            return Array.Empty<byte>();
        }
    }
}
