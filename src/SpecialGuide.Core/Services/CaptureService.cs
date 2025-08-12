using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace SpecialGuide.Core.Services;

public class CaptureService
{
    public virtual byte[] CaptureScreen()
    {
        var width = (int)SystemParameters.PrimaryScreenWidth;
        var height = (int)SystemParameters.PrimaryScreenHeight;
        using var bmp = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
