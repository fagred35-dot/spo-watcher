using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Поле ввода с кастомной отрисовкой: скругление, плавная подсветка фокуса.</summary>
public sealed class ModernTextBox : TextBox
{
    private bool _focused;
    private bool _hover;
    public int CornerRadius { get; set; } = 8;
    public Color BorderNormal { get; set; } = Theme.Border;
    public Color BorderFocus { get; set; } = Theme.Accent;
    public Color BorderHover { get; set; } = Theme.BorderHeavy;

    public ModernTextBox()
    {
        BorderStyle = BorderStyle.None;
        BackColor = Theme.Surface2;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10f);
        Padding = new Padding(12, 8, 12, 8);
        Height = 40;
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint
               | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _focused = true;
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _focused = false;
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    private GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        if (d > 0)
        {
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        }
        else
        {
            path.AddRectangle(r);
        }
        path.CloseFigure();
        return path;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == 0x000F) // WM_PAINT
        {
            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var border = _focused ? BorderFocus : (_hover ? BorderHover : BorderNormal);
            var borderW = _focused ? 1.5f : 1f;

            var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using var path = RoundedRect(rect, CornerRadius);
            using var pen = new Pen(border, borderW);
            g.DrawPath(pen, path);
        }
    }
}
