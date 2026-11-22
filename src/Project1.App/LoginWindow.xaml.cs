using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Interfaces;
using Project1.Data;
using Project1.Services;

namespace Project1.App;

public partial class LoginWindow : Window
{
    private readonly IAuthService? _authService;
    private readonly AppDbContext? _db;

    public LoginWindow(IAuthService authService, AppDbContext db)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (_authService == null || _db == null)
        {
            ShowError("Ошибка системы: Сервисы авторизации недоступны.");
            return;
        }

        string login = TxtLogin.Text.Trim();
        string password = _isPasswordVisible ? TxtPasswordVisible.Text : TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(login))
        {
            ShowError("Введите логин.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Введите пароль.");
            return;
        }

        BtnLogin.IsEnabled = false;
        BtnLogin.Content = "ВХОД...";
        // LoadingSpinner.Visibility = Visibility.Visible; // УБРАЛИ

        try
        {
            var result = await _authService.LoginAsync(login, password);

            if (result.Success && result.User != null)
            {
                var auditService = new AuditService(_db);
                await auditService.LogAsync(result.User.Id, "Login", $"Успешный вход в систему (Логин: {login}, Роль: {result.User.Role})");

                var mainWindow = new MainWindow(result.User, _db);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Неверный логин или пароль.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка соединения:\n{ex.Message}");
        }
        finally
        {
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "ВОЙТИ";
            // LoadingSpinner.Visibility = Visibility.Collapsed; // УБРАЛИ
        }
    }

    private async void BtnGuestLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        BtnGuestLogin.IsEnabled = false;
        BtnGuestLogin.Content = "Загрузка...";
        // LoadingSpinner.Visibility = Visibility.Visible; // УБРАЛИ

        try
        {
            var guestUser = await _db.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Role == Core.Enums.UserRole.User);

            if (guestUser != null)
            {
                var auditService = new AuditService(_db);
                await auditService.LogAsync(guestUser.Id, "Login", $"Гостевой вход как студент (Логин: {guestUser.Login})");

                var mainWindow = new MainWindow(guestUser, _db);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ShowError("В системе нет ни одного студента для гостевого входа.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка гостевого входа: {ex.Message}");
        }
        finally
        {
            BtnGuestLogin.IsEnabled = true;
            BtnGuestLogin.Content = "Войти как студент (Гость)";
            // LoadingSpinner.Visibility = Visibility.Collapsed; // УБРАЛИ
        }
    }

    private void ShowError(string msg)
    {
        TxtError.Text = msg;
        TxtError.Visibility = Visibility.Visible;
    }

    private bool _isPasswordVisible = false;

    private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            TxtPassword.Password = TxtPasswordVisible.Text;
            TxtPasswordVisible.Visibility = Visibility.Collapsed;
            TxtPassword.Visibility = Visibility.Visible;
            BtnTogglePassword.Content = "👁";
        }
        else
        {
            TxtPasswordVisible.Text = TxtPassword.Password;
            TxtPasswordVisible.Visibility = Visibility.Visible;
            TxtPassword.Visibility = Visibility.Collapsed;
            BtnTogglePassword.Content = "🙈";
        }
        _isPasswordVisible = !_isPasswordVisible;
    }
}