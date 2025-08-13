using SpecialGuide.Core.Models;
using System;
using System.Runtime.InteropServices;

namespace SpecialGuide.Core.Services;

public class OverlayService
{
    private readonly IRadialMenu _menu;
    private double _x;
    private double _y;

    public event EventHandler? CancelRequested;

    public OverlayService(IRadialMenu menu)
    {
        _menu = menu;
        _menu.CancelRequested += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowLoadingAtCursor()
    {
        var pos = GetCursorPosition();
        _x = pos.X;
        _y = pos.Y;
        _menu.ShowLoading();
        _menu.Show(_x, _y);
    }

    public void ShowSuggestions(string[] suggestions)
    {
        _menu.Populate(suggestions);
        _menu.Show(_x, _y);
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
