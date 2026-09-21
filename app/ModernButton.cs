using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Кнопка в стиле тёмной темы: без границ, со скруглением и hover.</summary>
public sealed class ModernButton : Button
{
    private bool _hover;
    private bool _pressed;

    public Color NormalColor { get; set; } = Theme.Surface2;
    public Color HoverColor { get; set; } = Color.FromArgb(0x3A, 0x3F, 0x44);
    public Color PressedColor { get; set; } = Color.FromArgb(0x2A, 0x2E, 0x32);
    public int CornerRadius { get; set; } = 6;
    public bool Accent { get; set; } = false;

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Theme.Surface2;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        Height = 32;
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint
               | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    private GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Surface);

        Color bg;
        if (!Enabled)
            bg = Color.FromArgb(0x2A, 0x2E, 0x32);
        else if (Accent)
            bg = _pressed ? Theme.Accent2 : (_hover ? Theme.Accent : Theme.Accent2);
        else
            bg = _pressed ? PressedColor : (_hover ? HoverColor : NormalColor);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, CornerRadius);
        using var brush = new SolidBrush(bg);
        g.FillPath(brush, path);

        var fg = Enabled ? (Accent ? Color.FromArgb(0x1A, 0x1A, 0x1A) : ForeColor) : Theme.TextDim;
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
