using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{
    private readonly SettingsService _settings;
    private IntPtr _hookId = IntPtr.Zero;
    private HookProc? _proc;
    private Keys _hotkey = Keys.None;
    private bool _overlayVisible;

    public event EventHandler? HotkeyPressed;

    public HookService(SettingsService settings)
    {
        _settings = settings;
        _settings.SettingsChanged += _ =>
        {
            try
            {
                Reload();
            }
            catch
            {
                // ignore reload failures
            }
        };
    }

    public void Start() => Reload();

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
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

    private IntPtr SetHook(HookProc proc, int idHook)
    {
        _proc = proc;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
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
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (kb.HasValue)
            {
                var key = (Keys)kb.Value.vkCode;
                var mods = Keys.None;
                if (IsKeyPressed(VK_CONTROL)) mods |= Keys.Control;
                if (IsKeyPressed(VK_SHIFT)) mods |= Keys.Shift;
                if (IsKeyPressed(VK_MENU)) mods |= Keys.Alt;
                var current = mods | key;
                if (current == _hotkey)
                {
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    if (_overlayVisible)
                    {
                        return new IntPtr(1);
                    }
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public static bool TryParseHotkey(string? hotkey, out Keys result)
    {
        result = Keys.None;
        if (string.IsNullOrWhiteSpace(hotkey))
            return false;
        var formatted = hotkey.Replace('+', ',');
        if (!Enum.TryParse(formatted, true, out result))
            return false;
        var key = result & Keys.KeyCode;
        if (key == Keys.None || key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu)
            return false;
        return true;
    }

    private static readonly Keys[] ReservedHotkeys =
    {
        Keys.Alt | Keys.Tab,
        Keys.Alt | Keys.F4,
        Keys.Control | Keys.Alt | Keys.Delete,
        Keys.Control | Keys.Shift | Keys.Escape
    };

    public static bool IsReservedHotkey(Keys hotkey) => Array.IndexOf(ReservedHotkeys, hotkey) >= 0;

    private static bool IsKeyPressed(int key) => (GetKeyState(key) & 0x8000) != 0;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
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
