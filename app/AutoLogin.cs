using System.IO;
using System.Reflection;

namespace SpoWatcher;

/// <summary>Скрипт автозаполнения формы входа. EmbeddedResource.</summary>
public static class AutoLogin
{
    private static string? _cached;

    public static string Script
    {
        get
        {
            if (_cached != null) return _cached;
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SpoWatcher.autologin.js")
                ?? throw new FileNotFoundException("autologin.js не встроен в сборку");
            using var reader = new StreamReader(stream);
            _cached = reader.ReadToEnd();
            return _cached;
        }
    }
}
