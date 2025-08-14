using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{
    private readonly SettingsService _settings;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private bool _overlayVisible;
    private Hotkey? _hotkey;

    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;

    public event EventHandler? HotkeyPressed;

    public HookService(SettingsService settings)
    {
        _settings = settings;
        _settings.SettingsChanged += _ => Reload();
    }

    public void Start() => Reload();

    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private void Reload()
    {
        Stop();
        _hotkey = null;
        RegisterMouseHook();
        var hotkeyString = _settings.ActivationHotkey;
        if (TryParseHotkey(hotkeyString, out var hotkey) && !IsReservedHotkey(hotkey))
        {
            _hotkey = hotkey;
            RegisterKeyboardHook();
        }
    }

    private void RegisterMouseHook()
    {
        _mouseProc ??= MouseHookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private void RegisterKeyboardHook()
    {
        _keyboardProc ??= KeyboardHookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public static bool TryParseHotkey(string? hotkey, out Hotkey result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(hotkey)) return false;
        Keys key = Keys.None;
        Keys mods = Keys.None;
        foreach (var part in hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.Trim().ToLowerInvariant())
            {
                case "control":
                case "ctrl":
                    mods |= Keys.Control;
                    break;
                case "shift":
                    mods |= Keys.Shift;
                    break;
                case "alt":
                    mods |= Keys.Alt;
                    break;
                default:
                    if (Enum.TryParse(part, true, out Keys parsedKey))
                    {
                        key = parsedKey;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
        }
        if (key == Keys.None) return false;
        result = new Hotkey(key, mods);
        return true;
    }

    public static bool IsReservedHotkey(Hotkey hotkey)
    {
        var (key, mods) = hotkey;
        if (mods.HasFlag(Keys.Alt) && key == Keys.Tab) return true;
        if (mods.HasFlag(Keys.Alt) && key == Keys.F4) return true;
        if (mods == (Keys.Control | Keys.Alt) && key == Keys.Delete) return true;
        return false;
    }

    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_MBUTTONDOWN = 0x0207;
        if (nCode >= 0 && wParam == (IntPtr)WM_MBUTTONDOWN)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            if (_overlayVisible)
            {
                return new IntPtr(1);
            }
        }
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        if (nCode >= 0 && _hotkey.HasValue && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (Keys)hookStruct.vkCode;
            var mods = GetCurrentModifiers();
            if (key == _hotkey.Value.Key && mods == _hotkey.Value.Modifiers)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                if (_overlayVisible)
                {
                    return new IntPtr(1);
                }
            }
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private static Keys GetCurrentModifiers()
    {
        Keys mods = Keys.None;
        if ((GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0) mods |= Keys.Control;
        if ((GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0) mods |= Keys.Shift;
        if ((GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0) mods |= Keys.Alt;
        return mods;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public readonly record struct Hotkey(Keys Key, Keys Modifiers);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
