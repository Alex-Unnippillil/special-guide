using System;
using System.Drawing;
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
    public void Redacts_Title_When_Enabled()
    {
        var settings = new Settings { CaptureMode = CaptureMode.ActiveWindow, RedactTitle = true };
        var service = new TestCaptureService(new SettingsService(settings), supported: true);
        service.CaptureScreen();
        Assert.True(service.RedactionCalled);
    }

    [Fact]
    public void Does_Not_Redact_When_Disabled()
    {
        var settings = new Settings { CaptureMode = CaptureMode.ActiveWindow, RedactTitle = false };
        var service = new TestCaptureService(new SettingsService(settings), supported: true);
        service.CaptureScreen();
        Assert.False(service.RedactionCalled);
    }

    private class TestCaptureService : CaptureService
    {
        private readonly bool _supported;
        private readonly SettingsService _settings;
        public bool ActiveWindowCalled { get; private set; }
        public bool FullScreenCalled { get; private set; }
        public bool RedactionCalled { get; private set; }

        public TestCaptureService(SettingsService settings, bool supported) : base(settings)
        {
            _settings = settings;
            _supported = supported;
        }

        protected override bool IsGraphicsCaptureSupported() => _supported;

        protected override byte[] CaptureActiveWindow()
        {
            ActiveWindowCalled = true;
            using var bmp = new Bitmap(100, 100);
            RedactTitleArea(bmp, IntPtr.Zero);
            return Array.Empty<byte>();
        }

        protected override byte[] CaptureFullScreen()
        {
            FullScreenCalled = true;
            return Array.Empty<byte>();
        }

        protected override void RedactTitleArea(Bitmap bmp, IntPtr hwnd)
        {
            if (_settings.Settings.RedactTitle)
                RedactionCalled = true;
        }
    }
}
