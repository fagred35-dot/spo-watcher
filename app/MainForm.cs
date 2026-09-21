using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SpoWatcher;

public sealed class MainForm : Form
{
    private const string SiteUrl = "https://spo.rso23.ru/students/student.html#/";
    private const string GradesHash = "#/reports/performance";
    private const string LessonsHash = "#/lessons";
    private const int DailyLessonsHour = 19;

    private readonly WebView2 _web;
    private readonly TextBox _log;
    private readonly Button _checkNow;
    private readonly Button _sendTomorrow;
    private readonly System.Windows.Forms.Timer _gradeTimer;
    private readonly System.Windows.Forms.Timer _dailyCheckTimer;

    private Snapshot? _prevGrades;
    private bool _gradesInitialized;
    private DateTime _lastLessonsSent = DateTime.MinValue;
    private Sender? _sender;
    private string _serverUrl = "";
    private string _secret = "";

    public MainForm()
    {
        Text = "СПО-Вотчер";
        Width = 1200;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        var top = new Panel { Dock = DockStyle.Top, Height = 32 };

        _checkNow = new Button { Text = "Проверить оценки", Width = 140, Left = 6, Top = 3 };
        _checkNow.Click += async (_, _) => await RunGradesCheckAsync();
        top.Controls.Add(_checkNow);

        _sendTomorrow = new Button { Text = "Расписание на завтра", Width = 170, Left = 156, Top = 3 };
        _sendTomorrow.Click += async (_, _) => await SendTomorrowLessonsAsync();
        top.Controls.Add(_sendTomorrow);

        var btnGrades = new Button { Text = "Оценки", Width = 100, Left = 336, Top = 3 };
        btnGrades.Click += (_, _) => NavigateTo(GradesHash);
        top.Controls.Add(btnGrades);

        var btnLessons = new Button { Text = "Расписание", Width = 110, Left = 446, Top = 3 };
        btnLessons.Click += (_, _) => NavigateTo(LessonsHash);
        top.Controls.Add(btnLessons);

        var btnSite = new Button { Text = "Главная", Width = 90, Left = 566, Top = 3 };
        btnSite.Click += (_, _) => NavigateTo("#/");
        top.Controls.Add(btnSite);

        _web = new WebView2 { Dock = DockStyle.Fill };

        _log = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Bottom,
            Height = 140,
            Font = new Font("Consolas", 9)
        };

        Controls.Add(_web);
        Controls.Add(_log);
        Controls.Add(top);

        // Оценки: раз в час
        _gradeTimer = new System.Windows.Forms.Timer { Interval = 3600 * 1000 };
        _gradeTimer.Tick += async (_, _) => await RunGradesCheckAsync();

        // Расписание: проверяем каждые 5 минут, не наступил ли 19:00
        _dailyCheckTimer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
        _dailyCheckTimer.Tick += async (_, _) => await MaybeSendDailyLessonsAsync();

        Load += async (_, _) => await InitAsync();
        FormClosing += (_, _) =>
        {
            try { _gradeTimer.Stop(); _dailyCheckTimer.Stop(); _sender?.Dispose(); } catch { }
        };

        LoadConfig();
    }

    private void LoadConfig()
    {
        _serverUrl = Environment.GetEnvironmentVariable("SPO_SERVER_URL") ?? "";
        _secret = Environment.GetEnvironmentVariable("SPO_SECRET") ?? "";

        try
        {
            var cfgPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(cfgPath))
            {
                var json = File.ReadAllText(cfgPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("server_url", out var su))
                    _serverUrl = su.GetString() ?? _serverUrl;
                if (doc.RootElement.TryGetProperty("secret", out var s))
                    _secret = s.GetString() ?? _secret;
            }
        }
        catch (Exception ex)
        {
            AppendLog("ошибка чтения config.json: " + ex.Message);
        }

        if (string.IsNullOrWhiteSpace(_serverUrl))
            AppendLog("ВНИМАНИЕ: server_url не задан — события будут только в лог.");
        else
            AppendLog("server_url = " + _serverUrl);

        _sender = new Sender(_serverUrl, _secret);
    }

    private async Task InitAsync()
    {
        try
        {
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "spo-watcher", "webview");
            Directory.CreateDirectory(userData);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            await _web.EnsureCoreWebView2Async(env);

            _web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _web.CoreWebView2.Navigate(SiteUrl);
            AppendLog("окно инициализировано, загружаю сайт");

            _gradeTimer.Start();
            _dailyCheckTimer.Start();
            AppendLog($"таймеры запущены: оценки раз в час, расписание ежедневно в {DailyLessonsHour}:00");
        }
        catch (Exception ex)
        {
            AppendLog("ошибка инициализации WebView2: " + ex.Message);
            MessageBox.Show("Не удалось запустить WebView2.\n" + ex.Message, "СПО-Вотчер",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void NavigateTo(string hash)
    {
        try { _web.CoreWebView2?.ExecuteScriptAsync($"location.hash = '{hash}';") ; }
        catch { }
    }

    private async Task<string> RunScraperAsync()
    {
        try { return await _web.CoreWebView2!.ExecuteScriptAsync(Scraper.Script); }
        catch (Exception ex) { AppendLog("ошибка скрапера: " + ex.Message); return ""; }
    }

    /// <summary>Раз в час: собрать оценки и сравнить.</summary>
    private async Task RunGradesCheckAsync()
    {
        if (_web.CoreWebView2 is null) { AppendLog("WebView2 не готов"); return; }
        try
        {
            _checkNow.Enabled = false;
            NavigateTo(GradesHash);
            await Task.Delay(1800);

            var json = await RunScraperAsync();
            if (string.IsNullOrEmpty(json)) { AppendLog("пустой ответ скрапера (оценки)"); return; }

            Snapshot snap;
            try { snap = JsonSerializer.Deserialize<Snapshot>(json) ?? new Snapshot(); }
            catch (Exception e) { AppendLog("плохой JSON (оценки): " + e.Message); return; }

            AppendLog($"оценок в снимке: {snap.Grades.Count}");

            if (!_gradesInitialized)
            {
                _gradesInitialized = true;
                _prevGrades = snap;
                AppendLog("первый снимок оценок сохранён");
                return;
            }

            var events = Diff.ComputeGrades(_prevGrades!, snap);
            _prevGrades = snap;

            if (events.Count == 0) { AppendLog("новых оценок нет"); return; }
            AppendLog($"новых оценок: {events.Count}");

            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(_serverUrl))
                {
                    AppendLog("  (не отправлено) " + JsonSerializer.Serialize(ev));
                    continue;
                }
                var ok = await _sender!.SendAsync(ev, AppendLog);
                AppendLog(ok ? $"  → отправлено: {ev.Subject} = {ev.Value}" : "  → ошибка отправки");
            }
        }
        finally { _checkNow.Enabled = true; }
    }

    /// <summary>Каждые 5 мин: если наступил час X и сегодня ещё не отправляли — шлём.</summary>
    private async Task MaybeSendDailyLessonsAsync()
    {
        var now = DateTime.Now;
        if (now.Hour != DailyLessonsHour) return;
        if (_lastLessonsSent.Date == now.Date) return;
        await SendTomorrowLessonsAsync();
    }

    /// <summary>Собрать расписание на завтра и отправить одним сообщением.</summary>
    private async Task SendTomorrowLessonsAsync()
    {
        if (_web.CoreWebView2 is null) { AppendLog("WebView2 не готов"); return; }
        try
        {
            _sendTomorrow.Enabled = false;
            AppendLog("собираю расписание на завтра");

            NavigateTo(LessonsHash);
            await Task.Delay(2000);

            var tomorrow = DateTime.Today.AddDays(1);
            var target = tomorrow.ToString("yyyy-MM-dd");

            // На странице — текущая неделя. Если завтра в другой неделе — надо переключиться.
            // Пробуем кликнуть «nextWeek» один раз, если завтра дальше воскресенья.
            var currentWeekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            var targetWeekStart = tomorrow.AddDays(-(((int)tomorrow.DayOfWeek + 6) % 7));
            if (targetWeekStart > currentWeekStart)
            {
                await _web.CoreWebView2.ExecuteScriptAsync(
                    "var el = document.querySelector('.icon-arrow-circle-right'); if (el) el.click();");
                await Task.Delay(1500);
            }

            var json = await RunScraperAsync();
            if (string.IsNullOrEmpty(json)) { AppendLog("пустой ответ скрапера (расписание)"); return; }

            Snapshot snap;
            try { snap = JsonSerializer.Deserialize<Snapshot>(json) ?? new Snapshot(); }
            catch (Exception e) { AppendLog("плохой JSON (расписание): " + e.Message); return; }

            var text = FormatLessonsForDate(snap, target);
            AppendLog($"пар на {target}: {(text == null ? 0 : text.Split('\n').Length)}");

            var ev = new Event
            {
                Kind = "lessons",
                Subject = $"Расписание на {tomorrow:dd.MM.yyyy}",
                Date = target,
                Value = text ?? "Пар нет"
            };

            if (string.IsNullOrWhiteSpace(_serverUrl))
            {
                AppendLog("  (не отправлено) " + ev.Value);
                return;
            }
            var ok = await _sender!.SendAsync(ev, AppendLog);
            if (ok) _lastLessonsSent = DateTime.Now;
            AppendLog(ok ? "  → расписание отправлено" : "  → ошибка отправки");
        }
        finally { _sendTomorrow.Enabled = true; }
    }

    /// <summary>Собирает список пар на конкретную дату в текст.</summary>
    private static string? FormatLessonsForDate(Snapshot snap, string isoDate)
    {
        var items = new List<(string time, Lesson lesson)>();
        foreach (var kv in snap.Lessons)
        {
            var parts = kv.Key.Split('|', 2);
            if (parts.Length < 2) continue;
            if (parts[0] != isoDate) continue;
            items.Add((parts[1], kv.Value));
        }
        if (items.Count == 0) return null;
        items.Sort((a, b) => string.CompareOrdinal(a.time, b.time));

        var sb = new StringBuilder();
        foreach (var (time, lesson) in items)
        {
            var end = string.IsNullOrEmpty(lesson.TimeEnd) ? "" : $"–{lesson.TimeEnd}";
            sb.Append($"{time}{end}  {lesson.Subject}");
            if (!string.IsNullOrEmpty(lesson.Room)) sb.Append($"\n    {lesson.Room}");
            if (!string.IsNullOrEmpty(lesson.Teacher)) sb.Append($"\n    {lesson.Teacher}");
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    private void AppendLog(string line)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        void add()
        {
            _log.AppendText($"[{ts}] {line}{Environment.NewLine}");
            if (_log.Lines.Length > 1000) _log.Lines = _log.Lines[^500..];
        }
        if (_log.InvokeRequired) _log.BeginInvoke((Action)add); else add();
    }
}
