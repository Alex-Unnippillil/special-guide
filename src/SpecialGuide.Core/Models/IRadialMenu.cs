namespace SpecialGuide.Core.Models;

public interface IRadialMenu
{
    event EventHandler? Cancelled;
    void Populate(string[] suggestions);
    void Show(double x, double y);
    void ShowLoading(double x, double y);
    void Hide();
}
