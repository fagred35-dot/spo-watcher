using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpoWatcher;

/// <summary>Снимок: оценки + расписание.</summary>
public sealed class Snapshot
{
    /// <summary>Оценки: ключ = "&lt;предмет&gt;|&lt;дата&gt;", значение = "5"/"4"/"3".</summary>
    [JsonPropertyName("grades")]
    public Dictionary<string, string> Grades { get; set; } = new();

    /// <summary>Пары: ключ = "&lt;дата&gt;|&lt;время&gt;", значение = инфа о паре.</summary>
    [JsonPropertyName("lessons")]
    public Dictionary<string, Lesson> Lessons { get; set; } = new();
}

public sealed class Lesson : IEquatable<Lesson>
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = "";
    [JsonPropertyName("teacher")] public string Teacher { get; set; } = "";
    [JsonPropertyName("room")]    public string Room    { get; set; } = "";
    [JsonPropertyName("time_end")] public string TimeEnd { get; set; } = "";

    public bool Equals(Lesson? other)
    {
        if (other is null) return false;
        return Subject == other.Subject
            && Teacher == other.Teacher
            && Room == other.Room
            && TimeEnd == other.TimeEnd;
    }

    public override bool Equals(object? obj) => Equals(obj as Lesson);
    public override int GetHashCode() => System.HashCode.Combine(Subject, Teacher, Room, TimeEnd);
}
