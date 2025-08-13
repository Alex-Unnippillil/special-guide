using SpecialGuide.Core.Services;
using SpecialGuide.Core.Models;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    [Fact]
    public void Registers_Keyboard_Hook_When_Hotkey_Configured()
    {
        var settings = new SettingsService(new Settings { ActivationHotkey = "Ctrl+Alt+H" });
        var service = new TestHookService(settings);
        service.Start();
        Assert.Equal(1, service.KeyboardHookCount);
        Assert.Equal(0, service.MouseHookCount);
    }

    [Fact]
    public void Falls_Back_To_Mouse_When_Keyboard_Fails()
    {
        var settings = new SettingsService(new Settings { ActivationHotkey = "Ctrl+Alt+H" });
        var service = new TestHookService(settings) { KeyboardShouldFail = true };
        service.Start();
        Assert.Equal(0, service.KeyboardHookCount);
        Assert.Equal(1, service.MouseHookCount);
    }

    [Fact]
    public void Stop_Unhooks_All()
    {
        var settings = new SettingsService(new Settings());
        var service = new TestHookService(settings);
        service.Start();
        service.Stop();
        Assert.Equal(0, service.KeyboardHookCount);
        Assert.Equal(0, service.MouseHookCount);
    }

    private class TestHookService : HookService
    {
        public int MouseHookCount { get; private set; }
        public int KeyboardHookCount { get; private set; }
        public bool KeyboardShouldFail { get; set; }

        public TestHookService(SettingsService settings) : base(settings) { }

        protected override IntPtr SetHook(int idHook, HookProc proc)
        {
            if (idHook == WH_KEYBOARD_LL)
            {
                if (KeyboardShouldFail) return IntPtr.Zero;
                KeyboardHookCount++;
                return new IntPtr(1);
            }
            else if (idHook == WH_MOUSE_LL)
            {
                MouseHookCount++;
                return new IntPtr(2);
            }
            return IntPtr.Zero;
        }

        protected override bool Unhook(IntPtr hookId)
        {
            if (hookId == new IntPtr(1)) KeyboardHookCount--;
            if (hookId == new IntPtr(2)) MouseHookCount--;
            return true;
        }
    }
}
