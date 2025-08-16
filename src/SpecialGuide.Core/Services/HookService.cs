using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpecialGuide.Core.Services;

/// <summary>
/// Manages global mouse and keyboard hooks used to activate the overlay.
/// Middle mouse click is always registered and an optional keyboard hotkey
/// can be configured through <see cref="SettingsService"/>.
/// </summary>
public class HookService : IDisposable
{
    private readonly SettingsService _settings;

    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private Hotkey? _hotkey;
    private bool _overlayVisible;

    public event EventHandler? HotkeyPressed;

    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;

    public HookService(SettingsService settings)
    {
        _settings = settings;
        _settings.SettingsChanged += _ => Reload();
    }

    /// <summary>
    /// Registers the mouse hook and initializes the keyboard hook based on the
    /// current configuration.
    /// </summary>
    public void Start()
    {
        Stop();

        _mouseProc = MouseHookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);

        Reload();
    }

    /// <summary>
    /// Unregisters any active hooks.
    /// </summary>
    public void Stop()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
            _mouseProc = null;
        }

        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
            _keyboardProc = null;
        }
    }

    /// <summary>
    /// Reloads the keyboard hook according to the current settings.
    /// </summary>
    private void Reload()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
            _keyboardProc = null;
        }

        if (TryParseHotkey(_settings.Settings.Hotkey, out var hotkey) && !IsReservedHotkey(hotkey))
        {
            _hotkey = hotkey;
            _keyboardProc = KeyboardHookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(curModule.ModuleName), 0);
        }
        else
        {
            _hotkey = null;
        }
    }

    private static bool TryParseHotkey(string value, out Hotkey result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var parts = value.Split('+', StringSplitOptions.RemoveEmptyEntries);
        Keys key = Keys.None;
        Keys mods = Keys.None;

        foreach (var part in parts)
        {
            var token = part.Trim();
            var lower = token.ToLowerInvariant();
            switch (lower)
            {
                case "ctrl":
                case "control":
                case "controlkey":
                    mods |= Keys.Control;
                    break;
                case "shift":
                case "shiftkey":
                    mods |= Keys.Shift;
                    break;
                case "alt":
                case "menu":
                    mods |= Keys.Alt;
                    break;
                default:
                    if (Enum.TryParse(token, true, out Keys parsed))
                    {
                        key = parsed;
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

    /// <summary>
    /// Indicates whether the overlay is currently visible. When visible we
    /// suppress the input that triggered it.
    /// </summary>
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
                // Swallow middle click when overlay is visible
                return new IntPtr(1);
            }
        }
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;

        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (Keys)hookStruct.vkCode;
            var mods = Control.ModifierKeys;

            if (_hotkey.HasValue && key == _hotkey.Value.Key && mods == _hotkey.Value.Modifiers)
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
    private static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }
}

