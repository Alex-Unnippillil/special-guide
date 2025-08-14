using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpecialGuide.Core.Services;

/// <summary>
/// Registers low level mouse and keyboard hooks used to activate the overlay.
/// </summary>
public class HookService : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;

    private readonly SettingsService _settings;

    private IntPtr _mouseHookId;
    private IntPtr _keyboardHookId;
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

    /// <summary>Initialises hooks based on the current settings.</summary>
    public void Start() => Reload();

    /// <summary>
    /// Re-reads settings and re-registers the keyboard hook if necessary.
    /// </summary>
    public void Reload()
    {
        Stop();

        // Always hook the middle mouse button.
        _mouseProc = MouseHookCallback;
        _mouseHookId = SetHook(_mouseProc, WH_MOUSE_LL);

        // Hotkey is optional.  Only register if it parses and isn't reserved.
        var hotkeyString = _settings.Settings.Hotkey;
        if (TryParseHotkey(hotkeyString, out var hk) && !IsReservedHotkey(hk))
        {
            _hotkey = hk;
            _keyboardProc = KeyboardHookCallback;
            _keyboardHookId = SetHook(_keyboardProc, WH_KEYBOARD_LL);
        }
        else
        {
            _hotkey = null;
        }
    }

    /// <summary>Unregisters any active hooks.</summary>
    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
            _keyboardProc = null;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
            _mouseProc = null;
        }
    }

    /// <summary>
    /// Indicates whether the overlay is currently visible.  When visible we
    /// swallow the activation input so that the underlying application doesn't
    /// also receive the event.
    /// </summary>
    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();

    private IntPtr SetHook(HookProc proc, int idHook)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule?.ModuleName), 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_MBUTTONDOWN = 0x0207;
        if (nCode >= 0 && wParam == (IntPtr)WM_MBUTTONDOWN)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            if (_overlayVisible)
            {
                // Returning a non-zero value prevents the event from being
                // passed to the next hook which stops the click reaching the
                // underlying application while the overlay is open.
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
            int vkCode = Marshal.ReadInt32(lParam);
            var key = (Keys)vkCode;

            var mods = HotkeyModifiers.None;
            if ((GetAsyncKeyState((int)Keys.ControlKey) & 0x8000) != 0)
                mods |= HotkeyModifiers.Control;
            if ((GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0)
                mods |= HotkeyModifiers.Shift;
            if ((GetAsyncKeyState((int)Keys.Menu) & 0x8000) != 0)
                mods |= HotkeyModifiers.Alt;

            if (_hotkey.Value.Key == key && _hotkey.Value.Modifiers == mods)
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

    // ---------------------------------------------------------------------
    // Hotkey helpers
    // ---------------------------------------------------------------------

    [Flags]
    public enum HotkeyModifiers
    {
        None = 0,
        Control = 1,
        Shift = 2,
        Alt = 4
    }

    public readonly record struct Hotkey(Keys Key, HotkeyModifiers Modifiers);

    /// <summary>Parses a hotkey string in the form "Control+Shift+K".</summary>
    public static bool TryParseHotkey(string? hotkey, out Hotkey result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(hotkey))
            return false;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Keys key = Keys.None;
        HotkeyModifiers mods = HotkeyModifiers.None;
        foreach (var part in parts)
        {
            if (Enum.TryParse(part, true, out HotkeyModifiers mod))
            {
                mods |= mod;
                continue;
            }

            if (Enum.TryParse(part, true, out Keys parsedKey))
            {
                key = parsedKey;
                continue;
            }

            // Unknown token.
            return false;
        }

        if (key == Keys.None)
            return false;

        result = new Hotkey(key, mods);
        return true;
    }

    /// <summary>
    /// Determines whether a hotkey is reserved by the operating system and
    /// should therefore not be used by the application.
    /// </summary>
    public static bool IsReservedHotkey(Hotkey hotkey)
    {
        // Alt+F4 closes windows.
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt) && hotkey.Key == Keys.F4)
            return true;

        // Ctrl+Alt+Del invokes the secure attention sequence.
        if (hotkey.Modifiers.HasFlag(HotkeyModifiers.Control) &&
            hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt) &&
            hotkey.Key == Keys.Delete)
            return true;

        return false;
    }

    // ---------------------------------------------------------------------
    // P/Invoke declarations
    // ---------------------------------------------------------------------

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}

