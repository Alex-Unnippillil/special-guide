using System.Runtime.InteropServices;

namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private HookProc? _proc;
    private bool _overlayVisible;

    public event EventHandler? MiddleClick;

    public void Start()
    {
        _hookId = SetHook(HookCallback);
        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to set Windows hook");
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();

    private IntPtr SetHook(HookProc proc)
    {
        _proc = proc;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_MBUTTONDOWN = 0x0207;
        if (nCode >= 0 && wParam == (IntPtr)WM_MBUTTONDOWN)
        {
            MiddleClick?.Invoke(this, EventArgs.Empty);
            if (_overlayVisible)
            {
                return new IntPtr(1); // suppress
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
