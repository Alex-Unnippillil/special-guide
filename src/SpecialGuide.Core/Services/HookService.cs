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

    private bool _overlayVisible;

    public event EventHandler? HotkeyPressed;

    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;


    {
        _settings = settings;
        _settings.SettingsChanged += _ => Reload();
        _setHook = setHook ?? SetWindowsHookEx;
        _unhook = unhook ?? UnhookWindowsHookEx;
    }

        }
        Reload();
    }


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

}
