using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Диалог ввода логина и пароля от сайта. Тёмная тема.</summary>
public sealed class SettingsForm : Form
{
    private readonly TextBox _login;
    private readonly TextBox _password;
    private readonly CheckBox _showPassword;
    private readonly Label _status;

    public SettingsForm()
    {
        Text = "Настройки";
        Width = 560;
        Height = 340;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10f);

        var title = new Label
        {
            Text = "Данные для входа на сайт",
            Left = 24, Top = 18, Width = 500, Height = 26,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Theme.Text
        };

        var lblLogin = new Label
        {
            Text = "Логин", Left = 24, Top = 60, Width = 500, Height = 18,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 9f)
        };
        _login = new TextBox
        {
            Left = 24, Top = 80, Width = 500, Height = 28,
            BackColor = Theme.Surface2, ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11f)
        };

        var lblPass = new Label
        {
            Text = "Пароль", Left = 24, Top = 120, Width = 500, Height = 18,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 9f)
        };
        _password = new TextBox
        {
            Left = 24, Top = 140, Width = 500, Height = 28,
            BackColor = Theme.Surface2, ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11f),
            UseSystemPasswordChar = true
        };

        _showPassword = new CheckBox
        {
            Text = "Показать пароль", Left = 24, Top = 176, Width = 200, Height = 24,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 9f)
        };
        _showPassword.CheckedChanged += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_showPassword.Checked;
        };

        var info = new Label
        {
            Left = 24, Top = 208, Width = 500, Height = 44,
            Text = "🔒 Пароль шифруется Windows (DPAPI) ключом текущего пользователя.\n" +
                   "Файл: %APPDATA%\\spo-watcher\\credentials.dat",
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 8.5f)
        };

        _status = new Label
        {
            Left = 24, Top = 254, Width = 500, Height = 22,
            ForeColor = Theme.Ok, Font = new Font("Segoe UI", 9f)
        };

        var btnSave = new ModernButton
        {
            Text = "Сохранить", Left = 284, Top = 282, Width = 120, Height = 34,
            Accent = true
        };
        btnSave.Click += (_, _) => Save();

        var btnClear = new ModernButton
        {
            Text = "Удалить", Left = 156, Top = 282, Width = 120, Height = 34
        };
        btnClear.Click += (_, _) => Clear();

        var btnClose = new ModernButton
        {
            Text = "Закрыть", Left = 412, Top = 282, Width = 110, Height = 34
        };
        btnClose.Click += (_, _) => Close();

        Controls.AddRange(new Control[]
        {
            title, lblLogin, _login, lblPass, _password, _showPassword,
            info, _status, btnSave, btnClear, btnClose
        });

        LoadStored();
    }

    private void LoadStored()
    {
        var data = Credentials.Load();
        if (data == null)
        {
            _status.Text = "Сохранённых данных нет.";
            _status.ForeColor = Theme.TextDim;
            return;
        }
        _login.Text = data.Login;
        _password.Text = data.Password;
        _status.Text = "Загружены сохранённые данные.";
        _status.ForeColor = Theme.TextDim;
    }

    private void Save()
    {
        var login = _login.Text.Trim();
        var password = _password.Text;
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            _status.Text = "Заполните и логин, и пароль.";
            _status.ForeColor = Theme.Err;
            return;
        }
        try
        {
            Credentials.Save(login, password);
            _status.Text = "Сохранено. При следующем запуске приложение введёт данные само.";
            _status.ForeColor = Theme.Ok;
        }
        catch (Exception ex)
        {
            _status.Text = "Ошибка сохранения: " + ex.Message;
            _status.ForeColor = Theme.Err;
        }
    }

    private void Clear()
    {
        Credentials.Delete();
        _login.Text = "";
        _password.Text = "";
        _status.Text = "Сохранённые данные удалены.";
        _status.ForeColor = Theme.TextDim;
    }
}
