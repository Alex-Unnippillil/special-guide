using System;
using System.Windows.Input;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    [Fact]
    public void TryParseHotkey_Parses_Modifier_Combinations()
    {
        var success = HookService.TryParseHotkey("Control+Shift+P", out var hotkey);
        Assert.True(success);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, hotkey.Modifiers);
        Assert.Equal(Key.P, hotkey.Key);
    }

    [Fact]
    public void IsReservedHotkey_Rejected()
    {
        HookService.TryParseHotkey("Control+Alt+Delete", out var hotkey);
        Assert.True(HookService.IsReservedHotkey(hotkey));
    }

    [Fact]
    public void StartStop_Manage_Hooks()
    {
        var service = new TestHookService();

        service.Start();
        Assert.True(service.IsKeyboardHookActive);
        Assert.True(service.IsMouseHookActive);
        Assert.Equal(1, service.KeyboardHookCount);
        Assert.Equal(1, service.MouseHookCount);

        service.Stop();
        Assert.False(service.IsKeyboardHookActive);
        Assert.False(service.IsMouseHookActive);
        Assert.Equal(0, service.KeyboardHookCount);
        Assert.Equal(0, service.MouseHookCount);
    }

    private class TestHookService : HookService
    {
        public int KeyboardHookCount { get; private set; }
        public int MouseHookCount { get; private set; }

        protected override IntPtr SetHook(HookProc proc, int idHook)
        {
            // WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14
            if (idHook == 13)
            {
                KeyboardHookCount++;
                return new IntPtr(1);
            }

            if (idHook == 14)
            {
                MouseHookCount++;
                return new IntPtr(2);
            }

            return IntPtr.Zero;
        }

        protected override bool RemoveHook(IntPtr hookId)
        {
            if (hookId == new IntPtr(1)) KeyboardHookCount--;
            if (hookId == new IntPtr(2)) MouseHookCount--;
            return true;
        }
    }
}
