namespace SpecialGuide.Core.Models;

public interface IRadialMenu
{
    void Populate(string[] suggestions);
    void Show(double x, double y);
    void Hide();
}
