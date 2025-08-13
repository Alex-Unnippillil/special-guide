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
        return _settings.Settings.CaptureMode == CaptureMode.ActiveWindow
            ? CaptureActiveWindow()
            : CaptureFullScreen();
    }

    protected virtual bool IsGraphicsCaptureAvailable() => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362);

    protected virtual byte[] CaptureActiveWindow()
    {
        var bounds = GetActiveWindowBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return Array.Empty<byte>();
        }

        if (IsGraphicsCaptureAvailable())
        {
            try
            {
                return CaptureWithGraphicsCapture(bounds);
            }
            catch
            {
                // Fall back to CopyFromScreen below
            }
        }

        return CaptureFromScreen(bounds);
    }

    protected virtual byte[] CaptureFullScreen()
    {
        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        return CaptureFromScreen(new Rectangle(left, top, width, height));
    }

    protected virtual Rectangle GetActiveWindowBounds()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return Rectangle.Empty;
        }
        if (!GetWindowRect(hwnd, out RECT rect))
        {
            return Rectangle.Empty;
        }
        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    protected virtual byte[] CaptureFromScreen(Rectangle bounds)
    {
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    protected virtual byte[] CaptureWithGraphicsCapture(Rectangle bounds)
    {
        // Placeholder implementation: a real implementation would use Windows.Graphics.Capture APIs
        // to capture the window. For now, fall back to CopyFromScreen to produce an image.
        return CaptureFromScreen(bounds);
    }

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
