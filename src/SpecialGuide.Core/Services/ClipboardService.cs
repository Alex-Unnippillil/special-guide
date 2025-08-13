using System.Runtime.InteropServices;
using System.Windows;

namespace SpecialGuide.Core.Services;

public class ClipboardService
{
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_V = 0x56;

    public bool AutoPaste { get; private set; }

    public ClipboardService(SettingsService settings)
    {
        UpdateAutoPaste(settings.Settings.AutoPaste);
        settings.SettingsChanged += s => UpdateAutoPaste(s.AutoPaste);
    }

    public void UpdateAutoPaste(bool autoPaste) => AutoPaste = autoPaste;

    public virtual void SetText(string text)
    {
        Clipboard.SetText(text);
        if (AutoPaste)
        {
            SendCtrlV();
        }
    }

    private static void SendCtrlV()
    {
        var inputs = new INPUT[]
        {
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL } } }, // Ctrl down
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V } } }, // V down
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_V, dwFlags = 2 } } }, // V up
            new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = VK_CONTROL, dwFlags = 2 } } } // Ctrl up
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
