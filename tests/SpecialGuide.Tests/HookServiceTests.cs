using SpecialGuide.Core.Models;
using SpecialGuide.Core.Services;
using SpecialGuide.Core.Models;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{
    [Fact]
    public void Registers_Keyboard_Hook_When_Hotkey_Configured()
    {

        {
            if (idHook == WH_KEYBOARD_LL)
            {
                if (KeyboardShouldFail) return IntPtr.Zero;
                KeyboardHookCount++;
                return new IntPtr(1);
            }
            else if (idHook == WH_MOUSE_LL)
            {
                MouseHookCount++;
                return new IntPtr(2);
            }
            return IntPtr.Zero;
        }

        protected override bool Unhook(IntPtr hookId)
        {
            if (hookId == new IntPtr(1)) KeyboardHookCount--;
            if (hookId == new IntPtr(2)) MouseHookCount--;
            return true;
        }
    }
}
