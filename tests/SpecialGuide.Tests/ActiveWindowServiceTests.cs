using System;
using System.Text;
using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class ActiveWindowServiceTests
{
    [Fact]
    public void Returns_Empty_When_No_Window()
    {
        var api = new FakeWin32Api(IntPtr.Zero, string.Empty);
        var service = new ActiveWindowService(api);
        var result = service.GetActiveWindowTitle();
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Returns_Title_From_Api()
    {
        var api = new FakeWin32Api(new IntPtr(1), "Notepad");
        var service = new ActiveWindowService(api);
        var result = service.GetActiveWindowTitle();
        Assert.Equal("Notepad", result);
    }

    private class FakeWin32Api : IWin32Api
    {
        private readonly IntPtr _handle;
        private readonly string _title;

        public FakeWin32Api(IntPtr handle, string title)
        {
            _handle = handle;
            _title = title;
        }

        public IntPtr GetForegroundWindow() => _handle;

        public int GetWindowText(IntPtr hWnd, StringBuilder text, int count)
        {
            text.Clear();
            text.Append(_title);
            return _title.Length;
        }
    }
}
