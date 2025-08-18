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
    private readonly Func<int, HookProc, IntPtr, uint, IntPtr> _setHook;
    private readonly Func<IntPtr, bool> _unhook;

    private IntPtr _mouseHookId;
    private IntPtr _keyboardHookId;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private Hotkey? _hotkey;

    private bool _overlayVisible;

    public event EventHandler? HotkeyPressed;

    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;

    public HookService(SettingsService settings,
        Func<int, HookProc, IntPtr, uint, IntPtr>? setHook = null,
        Func<IntPtr, bool>? unhook = null)
    {
        _settings = settings;
        _settings.SettingsChanged += _ => Reload();
        _setHook = setHook ?? SetWindowsHookEx;
        _unhook = unhook ?? UnhookWindowsHookEx;
        Reload();
    }

    /// <summary>
    /// Registers the global hooks.
    /// </summary>
    public void Start()
    {
        if (_mouseHookId == IntPtr.Zero)
        {
            _mouseProc = MouseHookCallback;
            var handle = GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);
            _mouseHookId = _setHook(WH_MOUSE_LL, _mouseProc, handle, 0);
        }

        if (_hotkey.HasValue && _keyboardHookId == IntPtr.Zero)
        {
            _keyboardProc = KeyboardHookCallback;
            var handle = GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);
            _keyboardHookId = _setHook(WH_KEYBOARD_LL, _keyboardProc, handle, 0);
        }
    }

    /// <summary>
    /// Unregisters any previously registered hooks.
    /// </summary>
    public void Stop()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            _unhook(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
            _mouseProc = null;
        }

        if (_keyboardHookId != IntPtr.Zero)
        {
            _unhook(_keyboardHookId);
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
            _unhook(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
            _keyboardProc = null;
        }

        if (TryParseHotkey(_settings.Settings.Hotkey, out var hk) && !IsReservedHotkey(hk))
        {
            _hotkey = hk;
        }
        else
        {
            _hotkey = null;
        }

        if (_hotkey.HasValue && _mouseHookId != IntPtr.Zero)
        {
            _keyboardProc = KeyboardHookCallback;
            var handle = GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName);
            _keyboardHookId = _setHook(WH_KEYBOARD_LL, _keyboardProc, handle, 0);
        }
    }

    internal static bool TryParseHotkey(string value, out Hotkey result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        Keys key = Keys.None;
        Keys mods = Keys.None;

        foreach (var part in value.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            var token = part.Trim();
            if (!Enum.TryParse<Keys>(token, true, out var parsed))
            {
                return false;
            }

            switch (parsed)
            {
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    mods |= Keys.Control;
                    break;
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    mods |= Keys.Shift;
                    break;
                case Keys.Alt:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                    mods |= Keys.Alt;
                    break;
                default:
                    key = parsed;
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
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && _hotkey.HasValue)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (Keys)hookStruct.vkCode;
            if (key == _hotkey.Value.Key)
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

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

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
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

