using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Диалог ввода логина и пароля. Современный Fluent-стиль.</summary>
public sealed class SettingsForm : Form
{
    private readonly ModernTextBox _login;
    private readonly ModernTextBox _password;
    private readonly CheckBox _showPassword;
    private readonly Label _status;
    private readonly Label _titleLabel;

    public SettingsForm()
    {
        Text = "Настройки";
        Width = 520;
        Height = 460;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = new Font("Segoe UI", 10f);

        // === Заголовок ===
        _titleLabel = new Label
        {
            Text = "Авторизация на сайте",
            Left = 28, Top = 24, Width = 460, Height = 32,
            Font = new Font("Segoe UI", 15f, FontStyle.Regular),
            ForeColor = Theme.Text
        };

        var subtitle = new Label
        {
            Text = "Эти данные используются только для автозаполнения формы входа",
            Left = 28, Top = 56, Width = 460, Height = 20,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 9f)
        };

        // === Логин ===
        var lblLogin = new Label
        {
            Text = "ЛОГИН", Left = 30, Top = 100, Width = 450, Height = 16,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        _login = new ModernTextBox
        {
            Left = 28, Top = 120, Width = 460, Height = 40,
            BackColor = Theme.Surface2
        };

        // === Пароль ===
        var lblPass = new Label
        {
            Text = "ПАРОЛЬ", Left = 30, Top = 172, Width = 450, Height = 16,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        _password = new ModernTextBox
        {
            Left = 28, Top = 192, Width = 460, Height = 40,
            BackColor = Theme.Surface2,
            UseSystemPasswordChar = true
        };

        // === Чекбокс ===
        _showPassword = new CheckBox
        {
            Text = " Показать пароль",
            Left = 28, Top = 242, Width = 240, Height = 24,
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 9f),
            FlatStyle = FlatStyle.Flat
        };
        _showPassword.FlatAppearance.CheckedBackColor = Theme.Surface2;
        _showPassword.FlatAppearance.BorderColor = Theme.Border;
        _showPassword.FlatAppearance.MouseOverBackColor = Theme.Surface2;
        _showPassword.CheckedChanged += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_showPassword.Checked;
        };

        // === Информационный блок ===
        var infoPanel = new Panel
        {
            Left = 28, Top = 284, Width = 460, Height = 64,
            BackColor = Theme.Surface2,
            Padding = new Padding(12, 8, 12, 8)
        };
        var infoLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "🔒  Данные хранятся зашифрованно через Windows DPAPI\n     %APPDATA%\\spo-watcher\\credentials.dat",
            ForeColor = Theme.TextDim,
            Font = new Font("Segoe UI", 9f),
            AutoSize = false
        };
        infoPanel.Controls.Add(infoLabel);
        infoPanel.Paint += (s, e) =>
        {
            var r = infoPanel.ClientRectangle;
            r.Inflate(-1, -1);
            using var p = new Pen(Theme.Border, 1);
            e.Graphics.DrawRectangle(p, r);
        };

        // === Статус ===
        _status = new Label
        {
            Left = 30, Top = 362, Width = 456, Height = 24,
            ForeColor = Theme.TextDim, Font = new Font("Segoe UI", 9f)
        };

        // === Кнопки ===
        var btnClose = new ModernButton
        {
            Text = "Закрыть",
            Left = 398, Top = 400, Width = 90, Height = 38,
            ButtonStyle = ModernButton.Style.Ghost
        };
        btnClose.Click += (_, _) => Close();

        var btnSave = new ModernButton
        {
            Text = "Сохранить",
            Left = 288, Top = 400, Width = 104, Height = 38,
            ButtonStyle = ModernButton.Style.Accent
        };
        btnSave.Click += (_, _) => Save();

        var btnClear = new ModernButton
        {
            Text = "Очистить",
            Left = 176, Top = 400, Width = 104, Height = 38,
            ButtonStyle = ModernButton.Style.Secondary
        };
        btnClear.Click += (_, _) => Clear();

        Controls.AddRange(new Control[]
        {
            _titleLabel, subtitle, lblLogin, _login, lblPass, _password,
            _showPassword, infoPanel, _status, btnSave, btnClear, btnClose
        });

        LoadStored();

        // Скругление углов формы
        Paint += (s, e) =>
        {
            using var p = new Pen(Theme.Border, 1);
            e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        };
    }

    private void LoadStored()
    {
        var data = Credentials.Load();
        if (data == null)
        {
            _status.Text = "● Сохранённых данных нет";
            _status.ForeColor = Theme.TextMut;
            return;
        }
        _login.Text = data.Login;
        _password.Text = data.Password;
        _status.Text = "● Загружены сохранённые данные";
        _status.ForeColor = Theme.Info;
    }

    private void Save()
    {
        var login = _login.Text.Trim();
        var password = _password.Text;
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            _status.Text = "✕ Заполните оба поля";
            _status.ForeColor = Theme.Err;
            return;
        }
        try
        {
            Credentials.Save(login, password);
            _status.Text = "✓ Данные сохранены";
            _status.ForeColor = Theme.Ok;
        }
        catch (Exception ex)
        {
            _status.Text = "✕ Ошибка: " + ex.Message;
            _status.ForeColor = Theme.Err;
        }
    }

    private void Clear()
    {
        Credentials.Delete();
        _login.Text = "";
        _password.Text = "";
        _status.Text = "● Данные удалены";
        _status.ForeColor = Theme.TextDim;
    }
}
