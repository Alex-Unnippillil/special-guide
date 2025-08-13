using System.Diagnostics;
using System.Runtime.InteropServices;


namespace SpecialGuide.Core.Services;

public class HookService : IDisposable
{


    internal bool IsMouseHookActive => _mouseHookId != IntPtr.Zero;
    internal bool IsKeyboardHookActive => _keyboardHookId != IntPtr.Zero;


        }
        RegisterMiddleClick();
    }

    private void Reload()
    {

        }
    }

    public void SetOverlayVisible(bool visible) => _overlayVisible = visible;

    public void Dispose() => Stop();
SetHook(HookProc proc, int idHook)


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

    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;


    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]

}
