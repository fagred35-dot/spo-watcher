using System.IO;
using System.Reflection;

namespace SpoWatcher;

/// <summary>JS-скрипт для инъекции в WebView2, встроен как EmbeddedResource.</summary>
public static class Scraper
{
    private static string? _cached;

    public static string Script
    {
        get
        {
            if (_cached != null) return _cached;
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SpoWatcher.scraper.js")
                ?? throw new FileNotFoundException("scraper.js не встроен в сборку");
            using var reader = new StreamReader(stream);
            _cached = reader.ReadToEnd();
            return _cached;
        }
    }
}
