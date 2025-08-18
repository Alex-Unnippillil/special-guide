using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    private int _keyboardHookCount;
    private int _mouseHookCount;

    private IntPtr RegisterHook(int idHook, HookService.HookProc proc, IntPtr hMod, uint threadId)
    {
        if (idHook == 13) // keyboard
        {
            _keyboardHookCount++;
            return new IntPtr(1);
        }
        if (idHook == 14) // mouse
        {
            _mouseHookCount++;
            return new IntPtr(2);
        }
        return IntPtr.Zero;
    }

    private bool UnregisterHook(IntPtr id)
    {
        if (id == new IntPtr(1)) _keyboardHookCount--;
        if (id == new IntPtr(2)) _mouseHookCount--;
        return true;
    }

    private static void RaiseSettingsChanged(SettingsService svc)
    {
        var field = typeof(SettingsService).GetField("SettingsChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        var del = (Action<Settings>?)field?.GetValue(svc);
        del?.Invoke(svc.Settings);
    }

    private HookService CreateService(Settings settings)
    {
        var svc = new SettingsService(settings);
        return new HookService(svc, RegisterHook, UnregisterHook);
    }

    [Fact]
    public void Start_And_Stop_Register_Hooks()
    {
        var service = CreateService(new Settings { Hotkey = "Alt+H" });
        service.Start();
        Assert.Equal(1, _mouseHookCount);
        Assert.Equal(1, _keyboardHookCount);
        service.Stop();
        Assert.Equal(0, _mouseHookCount);
        Assert.Equal(0, _keyboardHookCount);
    }

    [Fact]
    public void Constructor_Parses_Initial_Hotkey()
    {
        var service = CreateService(new Settings { Hotkey = "Control+K" });
        var field = typeof(HookService).GetField("_hotkey", BindingFlags.Instance | BindingFlags.NonPublic);
        var value = (HookService.Hotkey?)field!.GetValue(service);
        Assert.NotNull(value);
        Assert.Equal(Keys.K, value!.Value.Key);
        Assert.Equal(Keys.Control, value.Value.Modifiers);
    }

    [Fact]
    public void Reload_Responds_To_Setting_Changes()
    {
        var settings = new Settings();
        var svc = new SettingsService(settings);
        var service = new HookService(svc, RegisterHook, UnregisterHook);

        service.Start();
        Assert.Equal(0, _keyboardHookCount);

        settings.Hotkey = "Alt+H";
        RaiseSettingsChanged(svc);
        Assert.Equal(1, _keyboardHookCount);

        settings.Hotkey = string.Empty;
        RaiseSettingsChanged(svc);
        Assert.Equal(0, _keyboardHookCount);
    }

    [Fact]
    public void HotkeyPressed_Fires_For_Mouse_Or_Hotkey()
    {
        var settings = new Settings { Hotkey = "K" };
        var svc = new SettingsService(settings);
        var service = new HookService(svc, RegisterHook, UnregisterHook);
        bool fired = false;
        service.HotkeyPressed += (_, _) => fired = true;
        service.Start();

        // simulate middle click
        var mouse = typeof(HookService).GetMethod("MouseHookCallback", BindingFlags.Instance | BindingFlags.NonPublic);
        mouse!.Invoke(service, new object[] { 0, (IntPtr)0x0207, IntPtr.Zero });
        Assert.True(fired);

        fired = false;
        // simulate hotkey press
        var kbdStruct = new KBDLLHOOKSTRUCT { vkCode = (uint)Keys.K };
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<KBDLLHOOKSTRUCT>());
        Marshal.StructureToPtr(kbdStruct, ptr, false);
        var keyboard = typeof(HookService).GetMethod("KeyboardHookCallback", BindingFlags.Instance | BindingFlags.NonPublic);
        keyboard!.Invoke(service, new object[] { 0, (IntPtr)0x0100, ptr });
        Marshal.FreeHGlobal(ptr);
        Assert.True(fired);
    }

    [Fact]
    public void TryParseHotkey_Parses_Modifiers_And_Key()
    {
        Assert.True(HookService.TryParseHotkey("Control+Shift+K", out var hotkey));
        Assert.Equal(Keys.K, hotkey.Key);
        Assert.Equal(Keys.Control | Keys.Shift, hotkey.Modifiers);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
