using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SpecialGuide.Core.Services;

public interface IWin32Api
{
    IntPtr GetForegroundWindow();
    int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}

public class Win32Api : IWin32Api
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    IntPtr IWin32Api.GetForegroundWindow() => GetForegroundWindow();

    int IWin32Api.GetWindowText(IntPtr hWnd, StringBuilder text, int count) => GetWindowText(hWnd, text, count);
}

public class ActiveWindowService
{
    private readonly IWin32Api _api;

    public ActiveWindowService(IWin32Api api)
    {
        _api = api;
    }

    public string GetActiveWindowTitle()
    {
        var handle = _api.GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(256);
        _api.GetWindowText(handle, sb, sb.Capacity);
        return sb.ToString();
    }
}
