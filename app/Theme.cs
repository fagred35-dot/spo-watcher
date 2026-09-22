using System.Drawing;

namespace SpoWatcher;

/// <summary>Палитра тёмной темы приложения (Fluent/Discord-стиль).</summary>
public static class Theme
{
    // Базовые фоны
    public static readonly Color Bg       = Color.FromArgb(0x11, 0x13, 0x15); // Самый тёмный — фон окна
    public static readonly Color Surface  = Color.FromArgb(0x1C, 0x1F, 0x22); // Панели, тулбар
    public static readonly Color Surface2 = Color.FromArgb(0x24, 0x28, 0x2C); // Карточки, input fields
    public static readonly Color Surface3 = Color.FromArgb(0x2D, 0x32, 0x37); // Hover/selected

    // Границы
    public static readonly Color Border      = Color.FromArgb(0x35, 0x3A, 0x40); // Тонкие разделители
    public static readonly Color BorderHeavy = Color.FromArgb(0x45, 0x4B, 0x52); // Активные границы

    // Текст
    public static readonly Color Text    = Color.FromArgb(0xEA, 0xEB, 0xED);
    public static readonly Color TextDim = Color.FromArgb(0x8A, 0x90, 0x98);
    public static readonly Color TextMut = Color.FromArgb(0x5C, 0x62, 0x6A); // Очень тихий

    // Акцент (тёплый оранжевый)
    public static readonly Color Accent      = Color.FromArgb(0xFF, 0x9F, 0x43);
    public static readonly Color AccentHover = Color.FromArgb(0xFF, 0xAF, 0x5C);
    public static readonly Color AccentPress = Color.FromArgb(0xE8, 0x8B, 0x2E);

    // Состояния
    public static readonly Color Ok      = Color.FromArgb(0x4C, 0xAF, 0x50);
    public static readonly Color Err     = Color.FromArgb(0xEF, 0x53, 0x50);
    public static readonly Color Info    = Color.FromArgb(0x42, 0xA5, 0xF5);

    // Лог — цвета строк
    public static readonly Color LogInfo    = Color.FromArgb(0xD0, 0xD4, 0xD8);
    public static readonly Color LogSuccess = Color.FromArgb(0x81, 0xC7, 0x84);
    public static readonly Color LogWarning = Color.FromArgb(0xFF, 0xCA, 0x28);
    public static readonly Color LogError   = Color.FromArgb(0xEF, 0x9A, 0x9A);
    public static readonly Color LogDebug   = Color.FromArgb(0x90, 0xA4, 0xAE);
    public static readonly Color LogTimestamp = Color.FromArgb(0x6B, 0x72, 0x7A);
}
