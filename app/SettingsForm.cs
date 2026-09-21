using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpoWatcher;

/// <summary>Диалог ввода логина и пароля от сайта.</summary>
public sealed class SettingsForm : Form
{
    private readonly TextBox _login;
    private readonly TextBox _password;
    private readonly CheckBox _showPassword;
    private readonly Label _status;

    public SettingsForm()
    {
        Text = "Настройки — данные для входа";
        Width = 520;
        Height = 280;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblLogin = new Label { Text = "Логин:", Left = 16, Top = 20, Width = 100 };
        _login = new TextBox { Left = 120, Top = 16, Width = 360 };

        var lblPass = new Label { Text = "Пароль:", Left = 16, Top = 60, Width = 100 };
        _password = new TextBox { Left = 120, Top = 56, Width = 360, UseSystemPasswordChar = true };

        _showPassword = new CheckBox { Text = "Показать пароль", Left = 120, Top = 88, Width = 160 };
        _showPassword.CheckedChanged += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_showPassword.Checked;
        };

        var info = new Label
        {
            Left = 16, Top = 120, Width = 480, Height = 40,
            Text = "Пароль шифруется Windows (DPAPI) ключом текущего пользователя.\n" +
                   "Файл: %APPDATA%\\spo-watcher\\credentials.dat",
            ForeColor = Color.DimGray
        };

        _status = new Label { Left = 16, Top = 170, Width = 480, Height = 20, ForeColor = Color.Green };

        var btnSave = new Button { Text = "Сохранить", Left = 240, Top = 200, Width = 120 };
        btnSave.Click += (_, _) => Save();

        var btnClear = new Button { Text = "Удалить", Left = 120, Top = 200, Width = 110 };
        btnClear.Click += (_, _) => Clear();

        var btnCancel = new Button { Text = "Закрыть", Left = 368, Top = 200, Width = 110, DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[]
        {
            lblLogin, _login, lblPass, _password, _showPassword,
            info, _status, btnSave, btnClear, btnCancel
        });

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        LoadStored();
    }

    private void LoadStored()
    {
        var data = Credentials.Load();
        if (data == null)
        {
            _status.Text = "Сохранённых данных нет.";
            _status.ForeColor = Color.DimGray;
            return;
        }
        _login.Text = data.Login;
        _password.Text = data.Password;
        _status.Text = "Загружены сохранённые данные.";
        _status.ForeColor = Color.DimGray;
    }

    private void Save()
    {
        var login = _login.Text.Trim();
        var password = _password.Text;
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            _status.Text = "Заполните и логин, и пароль.";
            _status.ForeColor = Color.Firebrick;
            return;
        }
        try
        {
            Credentials.Save(login, password);
            _status.Text = "Сохранено. При следующем запуске приложение введёт данные само.";
            _status.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            _status.Text = "Ошибка сохранения: " + ex.Message;
            _status.ForeColor = Color.Firebrick;
        }
    }

    private void Clear()
    {
        Credentials.Delete();
        _login.Text = "";
        _password.Text = "";
        _status.Text = "Сохранённые данные удалены.";
        _status.ForeColor = Color.DimGray;
    }
}
