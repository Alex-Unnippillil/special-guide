using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class RadialMenuServiceTests
{
    [Theory]
    [InlineData(1,0,0,0,4,0)]
    [InlineData(0,1,0,0,4,1)]
    [InlineData(-1,0,0,0,4,2)]
    [InlineData(0,-1,0,0,4,3)]
    public void HitTest_Returns_Correct_Slice(double x, double y, double cx, double cy, int count, int expected)
    {
        var svc = new RadialMenuService();
        var slice = svc.HitTest(x, y, cx, cy, count);
        Assert.Equal(expected, slice);
    }
}
