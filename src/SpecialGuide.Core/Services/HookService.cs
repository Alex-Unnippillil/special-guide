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

    private Keys _hotkey = Keys.None;
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
    /// Starts listening for the middle mouse button and configured hotkey.
    /// </summary>
    public void Start()
    {
        Stop();
        RegisterMiddleClick();
        Reload();
    }

    /// <summary>
    /// Removes all hooks.
    /// </summary>
    public void Stop()
    {
        if (IsMouseHookActive)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        if (IsKeyboardHookActive)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Reloads the keyboard hook according to the current settings.
    /// </summary>
    private void Reload()
    {
        // remove existing keyboard hook
        if (IsKeyboardHookActive)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (TryParseHotkey(_settings.Settings.Hotkey, out var parsed) && !IsReservedHotkey(parsed))
        {
            _hotkey = parsed;
            _keyboardProc = KeyboardHookCallback;
            _keyboardHookId = SetHook(_keyboardProc, WH_KEYBOARD_LL);
        }
        else
        {
            _hotkey = Keys.None;
        }
    }

    /// <summary>
    /// Indicates whether the overlay is currently visible. When visible we
    /// suppress the input that triggered it.
    /// </summary>
    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();

    private void RegisterMiddleClick()
    {
        if (!IsMouseHookActive)
        {
            _mouseProc = MouseHookCallback;
            _mouseHookId = SetHook(_mouseProc, WH_MOUSE_LL);
        }
    }

    private IntPtr SetHook(HookProc proc, int idHook)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName!), 0);
    }

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
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            Keys modifiers = Keys.None;
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) modifiers |= Keys.Control;
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) modifiers |= Keys.Shift;
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) modifiers |= Keys.Alt;

            var combo = key | modifiers;
            if (combo == _hotkey)
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

    /// <summary>
    /// Attempts to parse a hotkey string (e.g. "Control+Shift+K") into
    /// a <see cref="Keys"/> value.
    /// </summary>
    public static bool TryParseHotkey(string? hotkey, out Keys parsed)
    {
        parsed = Keys.None;
        if (string.IsNullOrWhiteSpace(hotkey)) return false;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Keys result = Keys.None;
        bool hasNonModifier = false;

        foreach (var part in parts)
        {
            if (Enum.TryParse<Keys>(part, true, out var key))
            {
                result |= key;
                if (key != Keys.Control && key != Keys.Shift && key != Keys.Alt)
                    hasNonModifier = true;
            }
            else
            {
                return false;
            }
        }

        if (!hasNonModifier)
            return false;

        parsed = result;
        return true;
    }

    /// <summary>
    /// Determines whether the supplied hotkey is reserved by the operating system.
    /// </summary>
    public static bool IsReservedHotkey(Keys hotkey)
    {
        if (hotkey == Keys.None) return true;

        var key = hotkey & Keys.KeyCode;
        var modifiers = hotkey & Keys.Modifiers;

        // Common reserved combinations that should not be overridden.
        if (key == Keys.Tab && modifiers.HasFlag(Keys.Alt)) return true;      // Alt+Tab
        if (key == Keys.F4 && modifiers.HasFlag(Keys.Alt)) return true;       // Alt+F4
        if (key == Keys.Escape && modifiers.HasFlag(Keys.Alt)) return true;   // Alt+Esc
        if (key == Keys.Space && modifiers.HasFlag(Keys.Alt)) return true;    // Alt+Space
        if (key == Keys.Delete && modifiers.HasFlag(Keys.Control) && modifiers.HasFlag(Keys.Alt)) return true; // Ctrl+Alt+Del

        return false;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;

    private const int VK_CONTROL = 0x11;
    private const int VK_SHIFT = 0x10;
    private const int VK_MENU = 0x12; // Alt

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
}

