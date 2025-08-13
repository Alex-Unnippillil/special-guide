using System.Runtime.InteropServices;
using SpecialGuide.Core.Services;

namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{
    private readonly SettingsService _settings;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private HookProc? _mouseProc;
    private HookProc? _keyboardProc;
    private bool _overlayVisible;
    private int _hotkeyVk;
    private bool _requireCtrl;
    private bool _requireAlt;
    private bool _requireShift;

    public bool UsingFallback { get; private set; }

    public event EventHandler? MiddleClick;

    public HookService(SettingsService settings)
    {
        _settings = settings;
    }

    public void Start() => Reload();

    public void Reload()
    {
        Stop();
        var hotkey = _settings.Settings.ActivationHotkey;
        if (!string.IsNullOrWhiteSpace(hotkey) && TryRegisterHotkey(hotkey))
        {
            UsingFallback = false;
            return;
        }
        RegisterMiddleClick();
    }

    public void Stop()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookNative(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookNative(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }
    }

    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();

    private bool TryRegisterHotkey(string hotkey)
    {
        if (!TryParseHotkey(hotkey))
        {
            Warn("Invalid hotkey");
            return false;
        }
        _keyboardProc = KeyboardHookCallback;
        _keyboardHookId = SetHookNative(WH_KEYBOARD_LL, _keyboardProc);
        if (_keyboardHookId == IntPtr.Zero)
        {
            Warn("Failed to register keyboard hook");
            return false;
        }
        return true;
    }

    private void RegisterMiddleClick()
    {
        UsingFallback = true;
        _mouseProc = MouseHookCallback;
        _mouseHookId = SetHookNative(WH_MOUSE_LL, _mouseProc);
        if (_mouseHookId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to set Windows hook");
        }
    }

    private bool TryParseHotkey(string hotkey)
    {
        _requireCtrl = _requireAlt = _requireShift = false;
        _hotkeyVk = 0;
        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            if (!seen.Add(part)) return false;
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                case "CONTROL":
                    _requireCtrl = true;
                    break;
                case "ALT":
                    _requireAlt = true;
                    break;
                case "SHIFT":
                    _requireShift = true;
                    break;
                default:
                    if (_hotkeyVk != 0) return false;
                    if (!TryGetVk(part, out _hotkeyVk)) return false;
                    break;
            }
        }
        if (_hotkeyVk == 0) return false;
        if (_requireCtrl && _requireAlt && !_requireShift && _hotkeyVk == VK_DELETE)
            return false; // reserved Ctrl+Alt+Delete
        return true;
    }

    private static bool TryGetVk(string token, out int vk)
    {
        vk = 0;
        if (token.Length == 1)
        {
            char c = char.ToUpperInvariant(token[0]);
            if (c >= 'A' && c <= 'Z') { vk = c; return true; }
            if (c >= '0' && c <= '9') { vk = c; return true; }
        }
        if (token.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(token[1..], out var fn) && fn >= 1 && fn <= 24)
        {
            vk = 0x70 + fn - 1;
            return true;
        }
        if (token.Equals("DELETE", StringComparison.OrdinalIgnoreCase)) { vk = VK_DELETE; return true; }
        return false;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_MBUTTONDOWN = 0x0207;
        if (nCode >= 0 && wParam == (IntPtr)WM_MBUTTONDOWN)
        {
            MiddleClick?.Invoke(this, EventArgs.Empty);
            if (_overlayVisible)
            {
                return new IntPtr(1);
            }
        }
        return CallNextNative(_mouseHookId, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == _hotkeyVk && ModifiersPressed())
            {
                MiddleClick?.Invoke(this, EventArgs.Empty);
                if (_overlayVisible)
                {
                    return new IntPtr(1);
                }
            }
        }
        return CallNextNative(_keyboardHookId, nCode, wParam, lParam);
    }

    private bool ModifiersPressed()
    {
        if (_requireCtrl && (GetAsyncKeyState(VK_CONTROL) & 0x8000) == 0) return false;
        if (_requireAlt && (GetAsyncKeyState(VK_MENU) & 0x8000) == 0) return false;
        if (_requireShift && (GetAsyncKeyState(VK_SHIFT) & 0x8000) == 0) return false;
        return true;
    }

    protected virtual IntPtr SetHookNative(int idHook, HookProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    protected virtual bool UnhookNative(IntPtr hook) => UnhookWindowsHookEx(hook);

    protected virtual IntPtr CallNextNative(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam) => CallNextHookEx(hook, nCode, wParam, lParam);

    protected virtual void Warn(string message) => Console.Error.WriteLine(message);

    protected delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_SHIFT = 0x10;
    private const int VK_DELETE = 0x2E;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
