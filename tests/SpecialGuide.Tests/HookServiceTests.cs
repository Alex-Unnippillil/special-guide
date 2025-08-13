using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    [Fact]
    public void StartStop_Repeated_Cycles_Safe()
    {
        using var settings = new SettingsService(new Settings());
        var service = new HookService(settings);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Throws<DllNotFoundException>(() => service.Start());
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            service.Start();
            service.Stop();
        }

        var field = typeof(HookService).GetField("_hookId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.Equal(IntPtr.Zero, (IntPtr)field!.GetValue(service)!);
    }
}
