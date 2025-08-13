using System;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    private class TestHookService : HookService
    {
        public int LastHookType { get; private set; }
        public int UnhookCount { get; private set; }
        public bool FailKeyboardHook { get; set; }

        public TestHookService(SettingsService settings) : base(settings) { }

        protected override IntPtr SetHookNative(int idHook, HookProc proc)
        {
            LastHookType = idHook;
            if (FailKeyboardHook && idHook == 13)
                return IntPtr.Zero;
            return new IntPtr(1);
        }

        protected override bool UnhookNative(IntPtr hook)
        {
            UnhookCount++;
            return true;
        }
    }

    [Fact]
    public void StartStop_Repeated_Cycles_Safe()
    {
        var service = new TestHookService(new SettingsService(new Settings()));
        for (int i = 0; i < 3; i++)
        {
            service.Start();
            service.Stop();
        }
        Assert.Equal(5, service.UnhookCount);
    }

    [Fact]
    public void UsesKeyboardHook_WhenHotkeyValid()
    {
        var settings = new Settings { ActivationHotkey = "Ctrl+A" };
        var service = new TestHookService(new SettingsService(settings));
        service.Start();
        Assert.False(service.UsingFallback);
        Assert.Equal(13, service.LastHookType);
    }

    [Fact]
    public void FallsBack_WhenHotkeyInvalid()
    {
        var settings = new Settings { ActivationHotkey = "Ctrl+Invalid" };
        var service = new TestHookService(new SettingsService(settings));
        service.Start();
        Assert.True(service.UsingFallback);
        Assert.Equal(14, service.LastHookType);
    }

    [Fact]
    public void FallsBack_WhenKeyboardHookFails()
    {
        var settings = new Settings { ActivationHotkey = "Ctrl+A" };
        var service = new TestHookService(new SettingsService(settings)) { FailKeyboardHook = true };
        service.Start();
        Assert.True(service.UsingFallback);
        Assert.Equal(14, service.LastHookType);
    }
}
