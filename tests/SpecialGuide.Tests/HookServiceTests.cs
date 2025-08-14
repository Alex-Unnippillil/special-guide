using System;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    private int _keyboardHookCount;
    private int _mouseHookCount;

    private IntPtr RegisterHook(HookService.HookProc proc, int idHook)
    {
        // WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14
        if (idHook == 13)
        {
            _keyboardHookCount++;
            return new IntPtr(1);
        }
        if (idHook == 14)
        {
            _mouseHookCount++;
            return new IntPtr(2);
        }
        return IntPtr.Zero;
    }

    private bool UnregisterHook(IntPtr hookId)
    {
        if (hookId == new IntPtr(1)) _keyboardHookCount--;
        if (hookId == new IntPtr(2)) _mouseHookCount--;
        return true;
    }

    [Fact]
    public void TryParseHotkey_Parses_Valid_Combos()
    {
        var result = HookService.TryParseHotkey("Control+Shift+P", out var parsed);
        Assert.True(result);
        Assert.False(HookService.IsReservedHotkey(parsed));
    }

    [Fact]
    public void IsReservedHotkey_Rejects_Duplicates_Or_Reserved()
    {
        HookService.TryParseHotkey("Control+Control+P", out var duplicate);
        Assert.True(HookService.IsReservedHotkey(duplicate));

        HookService.TryParseHotkey("Alt+F4", out var reserved);
        Assert.True(HookService.IsReservedHotkey(reserved));
    }

    [Fact]
    public void Start_Stop_UpdateHookIds()
    {
        var settings = new Settings();
        var service = new HookService(new SettingsService(settings), RegisterHook, UnregisterHook);

        service.Start();
        Assert.Equal(1, _keyboardHookCount);
        Assert.Equal(1, _mouseHookCount);

        service.Stop();
        Assert.Equal(0, _keyboardHookCount);
        Assert.Equal(0, _mouseHookCount);
    }
}

