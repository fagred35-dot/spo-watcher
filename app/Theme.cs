using System.Drawing;

namespace SpoWatcher;

/// <summary>Палитра тёмной темы приложения.</summary>
public static class Theme
{
    public static readonly Color Bg        = Color.FromArgb(0x18, 0x1A, 0x1B);
    public static readonly Color Surface   = Color.FromArgb(0x23, 0x26, 0x29);
    public static readonly Color Surface2  = Color.FromArgb(0x2B, 0x2F, 0x33);
    public static readonly Color Border    = Color.FromArgb(0x3A, 0x3F, 0x44);
    public static readonly Color Text      = Color.FromArgb(0xE6, 0xE6, 0xE6);
    public static readonly Color TextDim   = Color.FromArgb(0x9A, 0xA0, 0xA6);
    public static readonly Color Accent    = Color.FromArgb(0xFF, 0x9F, 0x43);
    public static readonly Color Accent2   = Color.FromArgb(0xE8, 0x8B, 0x2E);
    public static readonly Color Ok        = Color.FromArgb(0x5C, 0xB8, 0x5C);
    public static readonly Color Err       = Color.FromArgb(0xE0, 0x6C, 0x75);
}
