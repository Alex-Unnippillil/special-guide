using System;

namespace SpecialGuide.Core.Models;

public interface IRadialMenu
{
    void Populate(string[] suggestions);
    void Show(double x, double y);
    void ShowLoading();
    void Hide();
    event EventHandler? CancelRequested;
}
