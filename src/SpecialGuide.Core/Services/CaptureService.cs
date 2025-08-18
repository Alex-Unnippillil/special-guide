using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using SpecialGuide.Core.Models;

namespace SpecialGuide.Core.Services;

public class CaptureService
{
    private readonly SettingsService _settings;

    public CaptureService(SettingsService settings)
    {
        _settings = settings;
    }

    public virtual byte[] CaptureScreen()
    {
        if (_settings.Settings.CaptureMode == CaptureMode.ActiveWindow && IsGraphicsCaptureSupported())
        {
            try
            {
                return CaptureActiveWindow();
            }
            catch
            {
                // Fall back to full screen
            }
        }

        if (_settings.Settings.CaptureMode == CaptureMode.CursorRegion)
        {
            try
            {
                return CaptureCursorRegion();
            }
            catch
            {
                // Fall back to full screen
            }
        }

        return CaptureFullScreen();
    }

    protected virtual bool IsGraphicsCaptureSupported()
    {
        if (!OperatingSystem.IsWindows()) return false;
        var t = Type.GetType("Windows.Graphics.Capture.GraphicsCaptureSession, Windows.Graphics.Capture, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");
        if (t == null) return false;
        var prop = t.GetProperty("IsSupported");
        return prop != null && (bool)prop.GetValue(null)!;
    }

    protected virtual byte[] CaptureActiveWindow()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return CaptureFullScreen();

        if (!GetWindowRect(hwnd, out RECT rect))
            return CaptureFullScreen();

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    protected virtual byte[] CaptureFullScreen()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var top = GetSystemMetrics(SM_YVIRTUALSCREEN);

        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(left, top, 0, 0, bmp.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    protected virtual byte[] CaptureCursorRegion(int width = 400, int height = 400)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        if (!GetCursorPos(out POINT cursor))
            return CaptureFullScreen();

        var left = cursor.X - width / 2;
        var top = cursor.Y - height / 2;

        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    protected struct POINT
    {
        public int X;
        public int Y;
    }
}
