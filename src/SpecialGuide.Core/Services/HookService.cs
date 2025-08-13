using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{
    private bool _overlayVisible;
    private Modifiers _hotkeyModifiers;
    private uint _hotkeyKey;
    private Modifiers _currentModifiers;

    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;

    public event EventHandler? HotkeyPressed;

    public HookService(SettingsService settings)

    }

    public void Start() => Reload();

    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            Unhook(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
        if (_mouseHookId != IntPtr.Zero)
        {
            Unhook(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private void Reload()
    {
        Stop();
        var hotkeyString = _settings.Settings.Hotkey;
        if (TryParseHotkey(hotkeyString, out var keys) && !IsReservedHotkey(keys))
        {
            _hotkey = keys;
            _hookId = SetHook(KeyboardCallback, WH_KEYBOARD_LL);
        }
        else
        {
            _hotkey = Keys.None;
            _hookId = SetHook(MouseCallback, WH_MOUSE_LL);
        }
        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to set Windows hook");
        }
    }

    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();
SetHook(HookProc proc, int idHook)

    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName), 0);
    }


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
        const int WM_KEYUP = 0x0101;
        const int WM_SYSKEYDOWN = 0x0104;
        const int WM_SYSKEYUP = 0x0105;
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            switch ((int)wParam)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    UpdateModifiers((int)data.vkCode, true);
                    if (data.vkCode == _hotkeyKey && _currentModifiers == _hotkeyModifiers)
                    {
                        MiddleClick?.Invoke(this, EventArgs.Empty);
                        if (_overlayVisible)
                        {
                            return new IntPtr(1);
                        }
                    }
                    break;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    UpdateModifiers((int)data.vkCode, false);
                    break;
            }
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private void UpdateModifiers(int vkCode, bool down)
    {
        void Set(ref Modifiers mods, Modifiers flag, bool value)
        {
            if (value) mods |= flag;
            else mods &= ~flag;
        }

        switch (vkCode)
        {
            case 0x10: // SHIFT
            case 0xA0:
            case 0xA1:
                Set(ref _currentModifiers, Modifiers.Shift, down);
                break;
            case 0x11: // CTRL
            case 0xA2:
            case 0xA3:
                Set(ref _currentModifiers, Modifiers.Control, down);
                break;
            case 0x12: // ALT
            case 0xA4:
            case 0xA5:
                Set(ref _currentModifiers, Modifiers.Alt, down);
                break;
            case 0x5B: // LWIN
            case 0x5C: // RWIN
                Set(ref _currentModifiers, Modifiers.Win, down);
                break;
        }
    }

    private static bool TryParseHotkey(string hotkey, out Modifiers modifiers, out uint key, out string? error)
    {
        modifiers = Modifiers.None;
        key = 0;
        error = null;
        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        bool keySet = false;
        foreach (var part in parts)
        {
            var token = part.Trim().ToUpperInvariant();
            switch (token)
            {
                case "CTRL":
                case "CONTROL":
                    if (modifiers.HasFlag(Modifiers.Control)) { error = "Duplicate modifier"; return false; }
                    modifiers |= Modifiers.Control;
                    break;
                case "ALT":
                    if (modifiers.HasFlag(Modifiers.Alt)) { error = "Duplicate modifier"; return false; }
                    modifiers |= Modifiers.Alt;
                    break;
                case "SHIFT":
                    if (modifiers.HasFlag(Modifiers.Shift)) { error = "Duplicate modifier"; return false; }
                    modifiers |= Modifiers.Shift;
                    break;
                case "WIN":
                case "WINDOWS":
                case "META":
                    if (modifiers.HasFlag(Modifiers.Win)) { error = "Duplicate modifier"; return false; }
                    modifiers |= Modifiers.Win;
                    break;
                default:
                    if (keySet) { error = "Multiple keys"; return false; }
                    if (token.Length == 1)
                    {
                        key = (uint)token[0];
                    }
                    else if (token.StartsWith("F") && int.TryParse(token.Substring(1), out var fn) && fn >= 1 && fn <= 12)
                    {
                        key = (uint)(0x70 + fn - 1);
                    }
                    else if (token == "DELETE")
                    {
                        key = 0x2E;
                    }
                    else
                    {
                        error = $"Unknown key '{part}'";
                        return false;
                    }
                    keySet = true;
                    break;
            }
        }
        if (!keySet)
        {
            error = "No key specified";
            return false;
        }
        if (modifiers == (Modifiers.Control | Modifiers.Alt) && key == 0x2E)
        {
            error = "Reserved combination";
            return false;
        }
        return true;
    }

    private static void Warn(string message) => Console.Error.WriteLine(message);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [Flags]
    private enum Modifiers
    {
        None = 0,
        Control = 1,
        Alt = 2,
        Shift = 4,
        Win = 8,
    }





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
