using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpoWatcher;

/// <summary>
/// Хранилище логина/пароля от сайта.
/// Шифруется Windows DPAPI (ProtectedData) ключом текущего пользователя.
/// Файл: %APPDATA%\spo-watcher\credentials.dat
/// Расшифровать может только этот пользователь на этой машине.
/// </summary>
public static class Credentials
{
    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "spo-watcher");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "credentials.dat");
        }
    }

    public sealed class Data
    {
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>Сохранить логин/пароль (шифрует DPAPI).</summary>
    public static void Save(string login, string password)
    {
        var data = new Data { Login = login, Password = password };
        var json = JsonSerializer.Serialize(data);
        var plain = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, encrypted);
    }

    /// <summary>Загрузить логин/пароль. null, если нет файла или не расшифровалось.</summary>
    public static Data? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var encrypted = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plain);
            return JsonSerializer.Deserialize<Data>(json);
        }
        catch
        {
            return null;
        }
    }

    public static bool Exists() => File.Exists(FilePath);

    public static void Delete()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
    }
}
