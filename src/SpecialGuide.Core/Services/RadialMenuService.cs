namespace SpecialGuide.Core.Services;

public class RadialMenuService
{
    public int HitTest(double x, double y, double centerX, double centerY, int itemCount)
    {
        var angle = Math.Atan2(y - centerY, x - centerX);
        if (angle < 0) angle += 2 * Math.PI;
        var slice = (int)(angle / (2 * Math.PI) * itemCount);
        return slice;
    }
}
