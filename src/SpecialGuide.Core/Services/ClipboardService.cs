using System.Runtime.InteropServices;
using System.Windows;

namespace SpecialGuide.Core.Services;

public class ClipboardService
{
    private readonly LoggingService _loggingService;

    public ClipboardService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public bool AutoPaste { get; set; }

    public bool SetText(string text)
    {
        try
        {
            SetClipboardText(text);
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Failed to set clipboard text.");
            return false;
        }

        if (AutoPaste && !SendCtrlV())
        {
            return false;
        }

        return true;
    }

    protected virtual void SetClipboardText(string text) => Clipboard.SetText(text);

    private bool SendCtrlV()
    {
        var inputs = new INPUT[]
        {
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x11 } } }, // Ctrl down
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x56 } } }, // V down
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x56, dwFlags = 2 } } }, // V up
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0x11, dwFlags = 2 } } } // Ctrl up
        };

        try
        {
            SendInputWrapper((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Failed to send Ctrl+V.");
            return false;
        }
    }

    protected virtual uint SendInputWrapper(uint nInputs, INPUT[] pInputs, int cbSize) => SendInput(nInputs, pInputs, cbSize);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    protected struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    protected struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
