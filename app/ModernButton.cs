using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Кнопка в стиле Fluent: скругление, варианты стилей, плавные состояния.</summary>
public sealed class ModernButton : Button
{
    public enum Style { Primary, Accent, Secondary, Ghost, Danger }

    private bool _hover;
    private bool _pressed;
    public Style ButtonStyle { get; set; } = Style.Secondary;
    public int CornerRadius { get; set; } = 8;

    /// <summary>Совместимость со старым кодом: Accent = true делает стиль Accent.</summary>
    public bool Accent
    {
        get => ButtonStyle == Style.Accent;
        set => ButtonStyle = value ? Style.Accent : Style.Secondary;
    }

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        Height = 36;
        Padding = new Padding(14, 0, 14, 0);
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.UserPaint
               | ControlStyles.ResizeRedraw
               | ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var parentBg = Parent?.BackColor ?? Theme.Surface;
        if (parentBg.A == 0) parentBg = Theme.Surface;
        g.Clear(parentBg);

        GetColors(out Color bg, out Color fg, out Color border);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = RoundedRect(rect, CornerRadius);

        // Тень для акцентных кнопок
        if (Enabled && (ButtonStyle == Style.Accent || ButtonStyle == Style.Primary))
        {
            using var shadowPath = RoundedRect(new RectangleF(rect.X + 1, rect.Y + 2, rect.Width, rect.Height), CornerRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var brush = new SolidBrush(bg))
            g.FillPath(brush, path);

        if (border.A > 0)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        var textRect = new Rectangle(0, 0, Width, Height);
        TextRenderer.DrawText(g, Text, Font, textRect, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    private void GetColors(out Color bg, out Color fg, out Color border)
    {
        border = Color.Transparent;

        if (!Enabled)
        {
            bg = Theme.Surface2;
            fg = Theme.TextMut;
            border = Theme.Border;
            return;
        }

        switch (ButtonStyle)
        {
            case Style.Accent:
                bg = _pressed ? Theme.AccentPress
                    : _hover ? Theme.AccentHover
                    : Theme.Accent;
                fg = Color.FromArgb(0x1A, 0x12, 0x08);
                break;

            case Style.Primary:
                bg = _pressed ? Color.FromArgb(0x34, 0x3A, 0x41)
                    : _hover ? Color.FromArgb(0x3D, 0x44, 0x4C)
                    : Color.FromArgb(0x2C, 0x31, 0x37);
                fg = Theme.Text;
                border = Theme.BorderHeavy;
                break;

            case Style.Secondary:
                bg = _pressed ? Theme.Surface3
                    : _hover ? Theme.Surface2
                    : Theme.Surface2;
                fg = Theme.Text;
                border = Theme.Border;
                break;

            case Style.Ghost:
                bg = _pressed ? Theme.Surface3
                    : _hover ? Color.FromArgb(28, 255, 255, 255)
                    : Color.Transparent;
                fg = _hover ? Theme.Accent : Theme.Text;
                break;

            case Style.Danger:
                bg = _pressed ? Color.FromArgb(0xC6, 0x28, 0x28)
                    : _hover ? Color.FromArgb(0xE5, 0x39, 0x35)
                    : Theme.Err;
                fg = Color.White;
                break;

            default:
                bg = Theme.Surface2;
                fg = Theme.Text;
                border = Theme.Border;
                break;
        }
    }
}
