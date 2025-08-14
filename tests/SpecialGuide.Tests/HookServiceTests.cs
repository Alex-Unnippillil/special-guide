using System;

using SpecialGuide.Core.Services;
using Xunit;

namespace SpecialGuide.Tests;

public class HookServiceTests
{

        }
        return IntPtr.Zero;
    }

    private bool UnregisterHook(IntPtr hookId)
    {
        if (hookId == new IntPtr(1)) _keyboardHookCount--;
        if (hookId == new IntPtr(2)) _mouseHookCount--;
        return true;
    }


    }
}

