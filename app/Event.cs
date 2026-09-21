using System.Text.Json.Serialization;

namespace SpoWatcher;

/// <summary>Событие для отправки на сервер. Без ПДн.</summary>
public sealed class Event
{
    [JsonPropertyName("type")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}
