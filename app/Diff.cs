using System.Collections.Generic;

namespace SpoWatcher;

public static class Diff
{
    /// <summary>Сравнивает только оценки. Возвращает список новых или изменившихся.</summary>
    public static List<Event> ComputeGrades(Snapshot prev, Snapshot cur)
    {
        var outList = new List<Event>();
        foreach (var kv in cur.Grades)
        {
            prev.Grades.TryGetValue(kv.Key, out var old);
            if (old == kv.Value) continue;
            var (subject, date) = SplitKey(kv.Key);
            outList.Add(new Event
            {
                Kind = "grade",
                Subject = subject,
                Date = date,
                Value = kv.Value
            });
        }
        return outList;
    }

    private static (string, string) SplitKey(string key)
    {
        var i = key.IndexOf('|');
        if (i < 0) return (key, "");
        return (key[..i], key[(i + 1)..]);
    }
}
