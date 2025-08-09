using SpecialGuide.Core.Models;
using System.Runtime.InteropServices;

namespace SpecialGuide.Core.Services;

public class OverlayService
{
    private readonly IRadialMenu _menu;

    public OverlayService(IRadialMenu menu)
    {
        _menu = menu;
    }

    public void ShowAtCursor(string[] suggestions)
    {
        var pos = GetCursorPosition();
        _menu.Populate(suggestions);
        _menu.Show(pos.X, pos.Y);
    }

    public void Hide() => _menu.Hide();

    private static (double X, double Y) GetCursorPosition()
    {
        POINT p;
        GetCursorPos(out p);
        return (p.X, p.Y);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private struct POINT
    {
        public int X;
        public int Y;
    }
}
