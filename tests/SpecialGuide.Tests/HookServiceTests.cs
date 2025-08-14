using System;
using System.Windows.Input;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    [Fact]
    public void TryParseHotkey_Parses_Valid_Combination()
    {
        var result = HookService.TryParseHotkey("Control+Shift+P", out var hotkey);
        Assert.True(result);
        Assert.Equal(Key.P, hotkey.Key);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, hotkey.Modifiers);
    }

    [Fact]
    public void IsReservedHotkey_Flags_Reserved_Combinations()
    {
        HookService.TryParseHotkey("Control+Alt+Delete", out var hotkey);
        Assert.True(HookService.IsReservedHotkey(hotkey));
    }

    [Fact]
    public void StartStop_Update_HookIds()
    {
        var service = new TestHookService();
        service.Start();
        Assert.True(service.IsKeyboardHookActive);
        Assert.True(service.IsMouseHookActive);
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
            if (idHook == 13) // WH_KEYBOARD_LL
            {
                KeyboardHookCount++;
                return new IntPtr(1);
            }
            if (idHook == 14) // WH_MOUSE_LL
            {
                MouseHookCount++;
                return new IntPtr(2);
            }
            return IntPtr.Zero;
        }

        protected override bool UnhookWindowsHookEx(IntPtr hookId)
        {
            if (hookId == new IntPtr(1))
            {
                KeyboardHookCount--;
            }
            else if (hookId == new IntPtr(2))
            {
                MouseHookCount--;
            }
            return true;
        }
    }
}
