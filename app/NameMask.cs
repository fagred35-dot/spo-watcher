using System.IO;
using System.Reflection;

namespace SpoWatcher;

/// <summary>JS-маскировка ФИО студента на сайте. EmbeddedResource.</summary>
public static class NameMask
{
    private static string? _cached;

    public static string Script
    {
        get
        {
            if (_cached != null) return _cached;
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SpoWatcher.name-mask.js")
                ?? throw new FileNotFoundException("name-mask.js не встроен в сборку");
            using var reader = new StreamReader(stream);
            _cached = reader.ReadToEnd();
            return _cached;
        }
    }
}
