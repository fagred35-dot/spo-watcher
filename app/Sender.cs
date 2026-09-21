using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpoWatcher;

public sealed class Sender : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _secret;

    public Sender(string url, string secret)
    {
        _url = url;
        _secret = secret;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    public async Task<bool> SendAsync(Event ev, Action<string> log)
    {
        try
        {
            var body = JsonSerializer.Serialize(ev);
            using var req = new HttpRequestMessage(HttpMethod.Post, _url);
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(_secret))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var hex = Convert.ToHexString(hash).ToLowerInvariant();
                req.Headers.Add("X-Signature", hex);
            }

            using var resp = await _http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                log($"ошибка сервера: HTTP {(int)resp.StatusCode}: {text}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            log("ошибка отправки: " + ex.Message);
            return false;
        }
    }

    public void Dispose() => _http.Dispose();
}
