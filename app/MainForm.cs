using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private readonly RichTextBox _log;
    private readonly ModernButton _checkNow;
    private readonly ModernButton _sendTomorrow;
    private readonly System.Windows.Forms.Timer _gradeTimer;
    private readonly System.Windows.Forms.Timer _dailyCheckTimer;

    private readonly Panel _logPanel;
    private readonly Panel _logHeader;
    private readonly Label _logTitle;
    private readonly Label _logToggle;
    private readonly Label _logBadge;
    private readonly Panel _statusBar;
    private readonly Panel _statusDot;
    private readonly Label _statusConn;
    private readonly Label _statusLast;
    private readonly Label _statusNext;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _uiTimer;
    private readonly DateTime _startedAt = DateTime.Now;

    private bool _logCollapsed;
    private const int LogHeight = 170;
    private const int LogCollapsedHeight = 34;
    private int _warnCount = 0;
    private int _errCount = 0;

    private Snapshot? _prevGrades;
    private bool _gradesInitialized;
    private DateTime _lastLessonsSent = DateTime.MinValue;
    private Sender? _sender;
    private string _serverUrl = "";
    private string _secret = "";
    private bool _autoLoginAttempted = false;

    public MainForm()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.3.0";
        Text = $"СПО-Вотчер v{ver}";
        Width = 1280;
        Height = 860;
        MinimumSize = new Size(960, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 9.75f);
        try { Icon = AppIcon.Make(); } catch { }

        // ==================== Toolbar ====================
        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Theme.Surface,
            Padding = new Padding(16, 0, 16, 0)
        };
        top.Paint += (s, e) =>
        {
            using var p = new Pen(Theme.Border, 1);
            e.Graphics.DrawLine(p, 0, top.Height - 1, top.Width, top.Height - 1);
        };

        var leftFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 0)
        };

        _checkNow = new ModernButton
        {
            Text = "⚡  Все оценки сейчас",
            Width = 180,
            ButtonStyle = ModernButton.Style.Accent,
            Margin = new Padding(0, 0, 8, 0)
        };
        _checkNow.Click += async (_, _) => await SendAllGradesAsync();
        leftFlow.Controls.Add(_checkNow);

        _sendTomorrow = new ModernButton
        {
            Text = "📅  Расписание",
            Width = 145,
            ButtonStyle = ModernButton.Style.Primary,
            Margin = new Padding(0, 0, 16, 0)
        };
        _sendTomorrow.Click += async (_, _) => await SendTomorrowLessonsAsync();
        leftFlow.Controls.Add(_sendTomorrow);

        var sep1 = new Panel { Width = 1, Height = 28, BackColor = Theme.Border, Margin = new Padding(0, 4, 16, 0) };
        leftFlow.Controls.Add(sep1);

        var btnGrades = new ModernButton { Text = "📚 Оценки", Width = 110, ButtonStyle = ModernButton.Style.Ghost, Margin = new Padding(0, 0, 4, 0) };
        btnGrades.Click += (_, _) => NavigateTo(GradesHash);
        leftFlow.Controls.Add(btnGrades);

        var btnLessons = new ModernButton { Text = "🗓 Расписание", Width = 130, ButtonStyle = ModernButton.Style.Ghost, Margin = new Padding(0, 0, 4, 0) };
        btnLessons.Click += (_, _) => NavigateTo(LessonsHash);
        leftFlow.Controls.Add(btnLessons);

        var btnSite = new ModernButton { Text = "🏠 Главная", Width = 110, ButtonStyle = ModernButton.Style.Ghost };
        btnSite.Click += (_, _) => NavigateTo("#/");
        leftFlow.Controls.Add(btnSite);

        top.Controls.Add(leftFlow);

        var rightFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 14, 0, 0)
        };
        var btnSettings = new ModernButton { Text = "⚙ Настройки", Width = 120, ButtonStyle = ModernButton.Style.Ghost };
        btnSettings.Click += (_, _) =>
        {
            using var dlg = new SettingsForm();
            dlg.ShowDialog(this);
        };
        rightFlow.Controls.Add(btnSettings);
        top.Controls.Add(rightFlow);

        // ==================== WebView2 ====================
        _web = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Theme.Bg
        };

        // ==================== Лог ====================
        _logPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = LogHeight,
            BackColor = Theme.Surface
        };

        _logHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Theme.Surface,
            Cursor = Cursors.Hand,
            Padding = new Padding(14, 0, 10, 0)
        };
        _logHeader.Paint += (s, e) =>
        {
            using var p = new Pen(Theme.Border, 1);
            e.Graphics.DrawLine(p, 0, 0, _logHeader.Width, 0);
        };

        _logToggle = new Label
        {
            Text = "▾",
            Dock = DockStyle.Right,
            Width = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        _logTitle = new Label
        {
            Text = "ЖУРНАЛ",
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        _logBadge = new Label
        {
            Text = "",
            Dock = DockStyle.Left,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Err,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(10, 0, 0, 0)
        };

        _logHeader.Controls.Add(_logToggle);
        _logHeader.Controls.Add(_logBadge);
        _logHeader.Controls.Add(_logTitle);

        _log = new RichTextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface2,
            ForeColor = Theme.LogInfo,
            BorderStyle = BorderStyle.None,
            Font = new Font("Cascadia Mono", 9.25f, FontStyle.Regular),
            Padding = new Padding(12, 10, 12, 10),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            DetectUrls = false,
            WordWrap = true,
            HideSelection = false
        };

        _logPanel.Controls.Add(_log);
        _logPanel.Controls.Add(_logHeader);

        // ==================== Статус-бар ====================
        _statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Theme.Surface
        };
        _statusBar.Paint += (s, e) =>
        {
            using var p = new Pen(Theme.Border, 1);
            e.Graphics.DrawLine(p, 0, 0, _statusBar.Width, 0);
        };

        var leftStat = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(14, 7, 0, 0)
        };

        _statusDot = new Panel
        {
            Width = 8,
            Height = 8,
            BackColor = Theme.TextMut,
            Margin = new Padding(2, 6, 8, 0)
        };
        _statusDot.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(_statusDot.BackColor);
            g.FillEllipse(brush, 0, 0, _statusDot.Width - 1, _statusDot.Height - 1);
            if (_statusDot.BackColor == Theme.Ok || _statusDot.BackColor == Theme.Err)
            {
                using var glow = new SolidBrush(Color.FromArgb(60, _statusDot.BackColor));
                g.FillEllipse(glow, -2, -2, _statusDot.Width + 3, _statusDot.Height + 3);
            }
        };

        _statusConn = new Label
        {
            Text = "сервер: —",
            AutoSize = true,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(0, 0, 16, 0)
        };

        var sepStatus = new Panel { Width = 1, Height = 14, BackColor = Theme.Border, Margin = new Padding(8, 4, 16, 0) };

        leftStat.Controls.Add(_statusDot);
        leftStat.Controls.Add(_statusConn);
        leftStat.Controls.Add(sepStatus);

        var rightStat = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 7, 14, 0)
        };

        _statusNext = new Label
        {
            Text = "следующая: —",
            AutoSize = true,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(16, 0, 0, 0)
        };

        _statusLast = new Label
        {
            Text = "последняя: —",
            AutoSize = true,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 8.5f)
        };

        rightStat.Controls.Add(_statusNext);
        rightStat.Controls.Add(_statusLast);

        _statusBar.Controls.Add(leftStat);
        _statusBar.Controls.Add(rightStat);

        // ==================== Сборка ====================
        Controls.Add(_web);
        Controls.Add(_logPanel);
        Controls.Add(_statusBar);
        Controls.Add(top);

        // ==================== Таймеры логики ====================
        _gradeTimer = new System.Windows.Forms.Timer { Interval = 3600 * 1000 };
        _gradeTimer.Tick += async (_, _) => await RunGradesCheckAsync();

        _dailyCheckTimer = new System.Windows.Forms.Timer { Interval = 5 * 60 * 1000 };
        _dailyCheckTimer.Tick += async (_, _) => await MaybeSendDailyLessonsAsync();

        _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uiTimer.Tick += (_, _) => UpdateStatus();
        _uiTimer.Start();

        // ==================== Лог: сворачивание ====================
        void ToggleLog()
        {
            _logCollapsed = !_logCollapsed;
            _log.Visible = !_logCollapsed;
            _logPanel.Height = _logCollapsed ? LogCollapsedHeight : LogHeight;
            _logToggle.Text = _logCollapsed ? "▸" : "▾";
        }
        _logHeader.Click += (_, _) => ToggleLog();
        _logTitle.Click += (_, _) => ToggleLog();
        _logToggle.Click += (_, _) => ToggleLog();
        _logBadge.Click += (_, _) => ToggleLog();

        // ==================== Трей ====================
        _tray = new NotifyIcon
        {
            Icon = Icon,
            Text = "СПО-Вотчер",
            Visible = true
        };
        var trayMenu = new ContextMenuStrip();
        trayMenu.BackColor = Theme.Surface;
        trayMenu.ForeColor = Theme.Text;
        trayMenu.Renderer = new DarkMenuRenderer();
        trayMenu.Items.Add("Открыть", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Выход", null, (_, _) =>
        {
            _tray.Visible = false;
            Close();
        });
        _tray.ContextMenuStrip = trayMenu;
        _tray.DoubleClick += (_, _) => RestoreFromTray();

        void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                try { _tray.ShowBalloonTip(1000, "СПО-Вотчер", "Приложение свёрнуто в трей", ToolTipIcon.Info); } catch { }
            }
        };

        Load += async (_, _) => await InitAsync();
        FormClosing += (_, _) =>
        {
            try
            {
                _gradeTimer.Stop();
                _dailyCheckTimer.Stop();
                _uiTimer.Stop();
                _sender?.Dispose();
                _tray.Visible = false;
            }
            catch { }
        };

        LoadConfig();

        try
        {
            var ok = !string.IsNullOrWhiteSpace(_serverUrl);
            _statusDot.BackColor = ok ? Theme.Ok : Theme.Err;
            _statusConn.Text = ok ? "сервер: подключён" : "сервер: не настроен";
            _statusConn.ForeColor = ok ? Theme.Ok : Theme.Err;
        }
        catch { }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            var now = DateTime.Now;
            var elapsed = (now - _startedAt).TotalMilliseconds;
            var cycles = Math.Floor(elapsed / 3600000.0) + 1;
            var nextGrades = _startedAt.AddMilliseconds(cycles * 3600000);

            var next19 = now.Date.AddHours(DailyLessonsHour);
            if (next19 <= now) next19 = next19.AddDays(1);

            _statusNext.Text = $"оценки {nextGrades:HH:mm}  ·  расписание {next19:HH:mm}";
        }
        catch { }
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
            AppendLog("ошибка чтения config.json: " + ex.Message, LogLevel.Error);
        }

        if (string.IsNullOrWhiteSpace(_serverUrl))
            AppendLog("server_url не задан — события только в лог", LogLevel.Warning);
        else
            AppendLog("server_url = " + _serverUrl, LogLevel.Info);

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
            _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            _web.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _web.CoreWebView2.Navigate(SiteUrl);
            AppendLog("окно инициализировано, загружаю сайт", LogLevel.Success);

            _gradeTimer.Start();
            _dailyCheckTimer.Start();
            AppendLog($"таймеры запущены: оценки 1ч, расписание ежедневно в {DailyLessonsHour}:00", LogLevel.Info);
        }
        catch (Exception ex)
        {
            AppendLog("ошибка инициализации WebView2: " + ex.Message, LogLevel.Error);
            MessageBox.Show("Не удалось запустить WebView2.\n" + ex.Message, "СПО-Вотчер",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void NavigateTo(string hash)
    {
        try { _web.CoreWebView2?.ExecuteScriptAsync($"location.hash = '{hash}';"); }
        catch { }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;

        try
        {
            var css = SiteTheme.Css.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
            await _web.CoreWebView2.ExecuteScriptAsync(
                "(function(){var id='spo-dark-theme';var old=document.getElementById(id);if(old)old.remove();" +
                "var s=document.createElement('style');s.id=id;s.type='text/css';s.textContent=`" + css + "`;" +
                "document.head.appendChild(s);})();");
        }
        catch (Exception ex)
        {
            AppendLog("ошибка инъекции CSS: " + ex.Message, LogLevel.Error);
        }

        if (_autoLoginAttempted) return;
        if (!Credentials.Exists()) return;

        var creds = Credentials.Load();
        if (creds == null || string.IsNullOrEmpty(creds.Login) || string.IsNullOrEmpty(creds.Password))
            return;

        try
        {
            var probe = await _web.CoreWebView2.ExecuteScriptAsync(
                "document.getElementById('login') ? 'yes' : 'no'");
            if (probe != "\"yes\"") return;

            _autoLoginAttempted = true;
            AppendLog("обнаружена форма входа, ввожу сохранённые данные", LogLevel.Info);

            var loginJson = JsonSerializer.Serialize(creds.Login);
            var passJson = JsonSerializer.Serialize(creds.Password);
            await _web.CoreWebView2.ExecuteScriptAsync(
                $"window.__spoCreds = {{ login: {loginJson}, password: {passJson} }};");

            var result = await _web.CoreWebView2.ExecuteScriptAsync(AutoLogin.Script);
            AppendLog("автозаполнение: " + result, LogLevel.Success);
        }
        catch (Exception ex)
        {
            AppendLog("ошибка автозаполнения: " + ex.Message, LogLevel.Error);
        }
    }

    private async Task<string> RunScraperAsync()
    {
        try { return await _web.CoreWebView2!.ExecuteScriptAsync(Scraper.Script); }
        catch (Exception ex) { AppendLog("ошибка скрапера: " + ex.Message, LogLevel.Error); return ""; }
    }

    private async Task RunGradesCheckAsync()
    {
        if (_web.CoreWebView2 is null) { AppendLog("WebView2 не готов", LogLevel.Error); return; }
        try
        {
            NavigateTo(GradesHash);
            await Task.Delay(1800);

            var json = await RunScraperAsync();
            if (string.IsNullOrEmpty(json)) { AppendLog("пустой ответ скрапера (оценки)", LogLevel.Warning); return; }

            Snapshot snap;
            try { snap = JsonSerializer.Deserialize<Snapshot>(json) ?? new Snapshot(); }
            catch (Exception e) { AppendLog("плохой JSON (оценки): " + e.Message, LogLevel.Error); return; }

            AppendLog($"оценок в снимке: {snap.Grades.Count}", LogLevel.Debug);

            if (!_gradesInitialized)
            {
                _gradesInitialized = true;
                _prevGrades = snap;
                AppendLog("первый снимок оценок сохранён", LogLevel.Info);
                return;
            }

            var events = Diff.ComputeGrades(_prevGrades!, snap);
            _prevGrades = snap;

            if (events.Count == 0) { AppendLog("новых оценок нет", LogLevel.Debug); return; }
            AppendLog($"новых оценок: {events.Count}", LogLevel.Success);

            foreach (var ev in events)
            {
                if (string.IsNullOrWhiteSpace(_serverUrl))
                {
                    AppendLog("  (не отправлено) " + JsonSerializer.Serialize(ev), LogLevel.Warning);
                    continue;
                }
                var ok = await _sender!.SendAsync(ev, (s) => AppendLog(s, LogLevel.Debug));
                AppendLog(ok ? $"  → отправлено: {ev.Subject} = {ev.Value}" : "  → ошибка отправки",
                    ok ? LogLevel.Success : LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            AppendLog("ошибка проверки оценок: " + ex.Message, LogLevel.Error);
        }
    }

    private async Task SendAllGradesAsync()
    {
        if (_web.CoreWebView2 is null) { AppendLog("WebView2 не готов", LogLevel.Error); return; }
        try
        {
            _checkNow.Enabled = false;
            NavigateTo(GradesHash);
            await Task.Delay(1800);

            var json = await RunScraperAsync();
            if (string.IsNullOrEmpty(json)) { AppendLog("пустой ответ скрапера (оценки)", LogLevel.Warning); return; }

            Snapshot snap;
            try { snap = JsonSerializer.Deserialize<Snapshot>(json) ?? new Snapshot(); }
            catch (Exception e) { AppendLog("плохой JSON (оценки): " + e.Message, LogLevel.Error); return; }

            if (snap.Grades.Count == 0) { AppendLog("оценок на странице нет", LogLevel.Warning); return; }
            AppendLog($"собрано оценок: {snap.Grades.Count}, отправляю всё", LogLevel.Info);

            _prevGrades = snap;
            _gradesInitialized = true;

            var allText = FormatAllGrades(snap);
            var ev = new Event
            {
                Kind = "grades_all",
                Subject = $"Все оценки на {DateTime.Today:dd.MM.yyyy}",
                Date = DateTime.Today.ToString("yyyy-MM-dd"),
                Value = allText
            };

            if (string.IsNullOrWhiteSpace(_serverUrl))
            {
                AppendLog("server_url не задан — не отправлено", LogLevel.Warning);
                return;
            }
            var ok = await _sender!.SendAsync(ev, (s) => AppendLog(s, LogLevel.Debug));
            AppendLog(ok ? "✓ все оценки отправлены" : "✕ ошибка отправки",
                ok ? LogLevel.Success : LogLevel.Error);
        }
        finally { _checkNow.Enabled = true; }
    }

    private static string FormatAllGrades(Snapshot snap)
    {
        var bySubject = new SortedDictionary<string, List<(string date, string value)>>();
        foreach (var kv in snap.Grades)
        {
            var parts = kv.Key.Split('|', 2);
            if (parts.Length < 2) continue;
            var subject = parts[0];
            var date = parts[1];
            if (!bySubject.TryGetValue(subject, out var list))
            {
                list = new List<(string, string)>();
                bySubject[subject] = list;
            }
            list.Add((date, kv.Value));
        }

        var sb = new StringBuilder();
        foreach (var kv in bySubject)
        {
            sb.AppendLine($"<b>{System.Net.WebUtility.HtmlEncode(kv.Key)}</b>");
            foreach (var (date, value) in kv.Value)
            {
                sb.AppendLine($"  {System.Net.WebUtility.HtmlEncode(date)} — {System.Net.WebUtility.HtmlEncode(value)}");
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private async Task MaybeSendDailyLessonsAsync()
    {
        var now = DateTime.Now;
        if (now.Hour != DailyLessonsHour) return;
        if (_lastLessonsSent.Date == now.Date) return;
        await SendTomorrowLessonsAsync();
    }

    private async Task SendTomorrowLessonsAsync()
    {
        if (_web.CoreWebView2 is null) { AppendLog("WebView2 не готов", LogLevel.Error); return; }
        try
        {
            _sendTomorrow.Enabled = false;
            AppendLog("собираю расписание на завтра", LogLevel.Info);

            NavigateTo(LessonsHash);
            await Task.Delay(2000);

            var tomorrow = DateTime.Today.AddDays(1);
            var target = tomorrow.ToString("yyyy-MM-dd");

            var currentWeekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            var targetWeekStart = tomorrow.AddDays(-(((int)tomorrow.DayOfWeek + 6) % 7));
            if (targetWeekStart > currentWeekStart)
            {
                await _web.CoreWebView2.ExecuteScriptAsync(
                    "var el = document.querySelector('.icon-arrow-circle-right'); if (el) el.click();");
                await Task.Delay(1500);
            }

            var json = await RunScraperAsync();
            if (string.IsNullOrEmpty(json)) { AppendLog("пустой ответ скрапера (расписание)", LogLevel.Warning); return; }

            Snapshot snap;
            try { snap = JsonSerializer.Deserialize<Snapshot>(json) ?? new Snapshot(); }
            catch (Exception e) { AppendLog("плохой JSON (расписание): " + e.Message, LogLevel.Error); return; }

            var text = FormatLessonsForDate(snap, target);
            AppendLog($"пар на {target}: {(text == null ? 0 : text.Split('\n').Length)}", LogLevel.Info);

            var ev = new Event
            {
                Kind = "lessons",
                Subject = $"Расписание на {tomorrow:dd.MM.yyyy}",
                Date = target,
                Value = text ?? "Пар нет"
            };

            if (string.IsNullOrWhiteSpace(_serverUrl))
            {
                AppendLog("server_url не задан — не отправлено", LogLevel.Warning);
                return;
            }
            var ok = await _sender!.SendAsync(ev, (s) => AppendLog(s, LogLevel.Debug));
            if (ok) _lastLessonsSent = DateTime.Now;
            AppendLog(ok ? "✓ расписание отправлено" : "✕ ошибка отправки",
                ok ? LogLevel.Success : LogLevel.Error);
        }
        finally { _sendTomorrow.Enabled = true; }
    }

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

        static string enc(string s) => System.Net.WebUtility.HtmlEncode(s);
        var sb = new StringBuilder();
        foreach (var (time, lesson) in items)
        {
            var end = string.IsNullOrEmpty(lesson.TimeEnd) ? "" : $"–{lesson.TimeEnd}";
            sb.AppendLine($"\u23F0 <b>{enc(time)}{enc(end)}</b>");
            sb.AppendLine($"\uD83D\uDCD8 {enc(lesson.Subject)}");
            if (!string.IsNullOrEmpty(lesson.Room)) sb.AppendLine($"\uD83D\uDEAA {enc(lesson.Room)}");
            if (!string.IsNullOrEmpty(lesson.Teacher)) sb.AppendLine($"\uD83D\uDC64 {enc(lesson.Teacher)}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private enum LogLevel { Debug, Info, Success, Warning, Error }

    private void AppendLog(string line, LogLevel level = LogLevel.Info)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        void add()
        {
            Color color;
            string prefix;
            switch (level)
            {
                case LogLevel.Debug:   color = Theme.LogDebug;   prefix = "DBG "; break;
                case LogLevel.Success: color = Theme.LogSuccess; prefix = "✓   "; break;
                case LogLevel.Warning: color = Theme.LogWarning; prefix = "!   "; _warnCount++; break;
                case LogLevel.Error:   color = Theme.LogError;   prefix = "✕   "; _errCount++; break;
                default:               color = Theme.LogInfo;    prefix = "·   "; break;
            }

            if (_log.TextLength > 50000)
            {
                _log.Select(0, _log.TextLength / 2);
                _log.SelectedText = "";
            }

            var atEnd = _log.SelectionStart >= _log.TextLength - 10;

            _log.SelectionStart = _log.TextLength;
            _log.SelectionLength = 0;
            _log.SelectionColor = Theme.LogTimestamp;
            _log.AppendText(ts + " ");
            _log.SelectionColor = color;
            _log.AppendText(prefix + line + "\n");

            if (atEnd) _log.ScrollToCaret();

            UpdateLogBadge();

            try
            {
                if (level == LogLevel.Success)
                {
                    if (line.Contains("оцен")) _statusLast.Text = "последняя: оценки " + ts;
                    else if (line.Contains("расписан")) _statusLast.Text = "последняя: расписание " + ts;
                }
            }
            catch { }
        }
        if (_log.InvokeRequired) _log.BeginInvoke((Action)add); else add();
    }

    private void UpdateLogBadge()
    {
        if (_errCount == 0 && _warnCount == 0)
        {
            _logBadge.Text = "";
            return;
        }
        var parts = new List<string>();
        if (_errCount > 0) parts.Add($"● {_errCount} err");
        if (_warnCount > 0) parts.Add($"● {_warnCount} warn");
        _logBadge.Text = string.Join("  ", parts);
        _logBadge.ForeColor = _errCount > 0 ? Theme.Err : Theme.LogWarning;
    }
}

/// <summary>Тёмная тема для выпадающих меню.</summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rc = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
        if (e.Item.Selected)
        {
            using var brush = new SolidBrush(Theme.Surface3);
            e.Graphics.FillRectangle(brush, rc);
        }
        else
        {
            using var brush = new SolidBrush(Theme.Surface);
            e.Graphics.FillRectangle(brush, rc);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextMut;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var rc = new Rectangle(6, e.Item.Height / 2, e.Item.Width - 12, 1);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, rc.Left, rc.Top, rc.Right, rc.Top);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Theme.Border);
        var rc = e.AffectedBounds;
        rc.Inflate(-1, -1);
        e.Graphics.DrawRectangle(pen, rc);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(brush, e.ToolStrip.ClientRectangle);
    }
}

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.Surface;
    public override Color ImageMarginGradientBegin => Theme.Surface;
    public override Color ImageMarginGradientMiddle => Theme.Surface;
    public override Color ImageMarginGradientEnd => Theme.Surface;
    public override Color SeparatorDark => Theme.Border;
    public override Color SeparatorLight => Theme.Border;
    public override Color MenuBorder => Theme.Border;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Theme.Surface3;
    public override Color MenuItemSelectedGradientBegin => Theme.Surface3;
    public override Color MenuItemSelectedGradientEnd => Theme.Surface3;
}
