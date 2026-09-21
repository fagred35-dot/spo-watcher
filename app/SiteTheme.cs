using System.IO;
using System.Reflection;

namespace SpoWatcher;

/// <summary>Тёмная CSS-тема для сайта, встроенная как EmbeddedResource.</summary>
public static class SiteTheme
{
    private static string? _cached;

    public static string Css
    {
        get
        {
            if (_cached != null) return _cached;
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SpoWatcher.site-dark.css")
                ?? throw new FileNotFoundException("site-dark.css не встроен в сборку");
            using var reader = new StreamReader(stream);
            _cached = reader.ReadToEnd();
            return _cached;
        }
    }
}
