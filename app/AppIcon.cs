using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Простая иконка приложения, нарисованная в рантайме.</summary>
public static class AppIcon
{
    public static Icon Make()
    {
        var bmp = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var back = new SolidBrush(Theme.Bg);
            g.FillEllipse(back, 0, 0, 63, 63);

            using var accent = new SolidBrush(Theme.Accent);
            g.FillEllipse(accent, 5, 5, 53, 53);

            using var font = new Font("Segoe UI", 30, FontStyle.Bold, GraphicsUnit.Pixel);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("С", font, Brushes.Black, new RectangleF(0, 2, 64, 64), sf);
        }

        var h = bmp.GetHicon();
        try { return Icon.FromHandle(h); }
        catch { return SystemIcons.Application; }
    }
}
