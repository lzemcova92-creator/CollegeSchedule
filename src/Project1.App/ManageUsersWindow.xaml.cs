using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Core.Enums;
using Project1.Core.Validation; // <-- Подключаем валидатор
using Project1.Data;
using Project1.Data.Security;

namespace Project1.App;

public partial class ManageUsersWindow : Window
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _hasher;
    private int? _editingUserId = null;

    public ManageUsersWindow(AppDbContext db, PasswordHasher hasher)
    {
        InitializeComponent();
        _db = db;
        _hasher = hasher;

        Loaded += async (s, e) => await LoadUsersAsync();
        CmbRole.ItemsSource = Enum.GetValues(typeof(UserRole));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _db.Users.Include(u => u.Group).ToListAsync();
            DgUsers.ItemsSource = users;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // Валидация
        if (!InputValidator.IsValidLogin(TxtLogin.Text))
        {
            MessageBox.Show("Логин должен быть от 3 до 20 символов (только латиница и цифры).", "Ошибка валидации");
            return;
        }

        if (!InputValidator.IsValidFullName(TxtFullName.Text))
        {
            MessageBox.Show("Введите корректное ФИО (минимум Имя и Фамилия).", "Ошибка валидации");
            return;
        }

        if (CmbRole.SelectedItem is not UserRole role)
        {
            MessageBox.Show("Выберите роль.", "Внимание");
            return;
        }

        try
        {
            string passwordToHash = TxtPassword.Text;

            // Если это новый пользователь или пароль был изменен
            if (_editingUserId == null || !string.IsNullOrWhiteSpace(passwordToHash))
            {
                if (role == UserRole.User && string.IsNullOrWhiteSpace(passwordToHash))
                {
                    passwordToHash = "123456"; // Дефолтный пароль для студентов
                }

                if (!InputValidator.IsValidPassword(passwordToHash))
                {
                    MessageBox.Show("Пароль должен содержать минимум 6 символов.", "Ошибка валидации");
                    return;
                }
            }

            if (_editingUserId.HasValue)
            {
                var user = await _db.Users.FindAsync(_editingUserId.Value);
                if (user == null) return;

                user.Login = TxtLogin.Text.Trim();
                user.FullName = TxtFullName.Text.Trim();
                user.Role = role;

                if (!string.IsNullOrWhiteSpace(passwordToHash))
                {
                    var result = _hasher.Hash(passwordToHash);
                    user.PasswordHash = result.Hash;
                    user.PasswordSalt = result.Salt;
                }

                _db.Users.Update(user);
                await _db.SaveChangesAsync();
                MessageBox.Show("Пользователь обновлен!", "Успех");
            }
            else
            {
                if (await _db.Users.AnyAsync(u => u.Login == TxtLogin.Text.Trim()))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка");
                    return;
                }

                var result = _hasher.Hash(passwordToHash);
                var newUser = new User
                {
                    Login = TxtLogin.Text.Trim(),
                    PasswordHash = result.Hash,
                    PasswordSalt = result.Salt,
                    FullName = TxtFullName.Text.Trim(),
                    Role = role
                };

                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();
                MessageBox.Show("Пользователь добавлен!", "Успех");
            }

            ResetForm();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgUsers.SelectedItem is not User selected) return;
        LoadUserToForm(selected);
    }

    private void DgUsers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgUsers.SelectedItem is User selected) LoadUserToForm(selected);
    }

    private void LoadUserToForm(User user)
    {
        _editingUserId = user.Id;
        FormTitle.Text = "✏️ Редактировать пользователя";
        BtnCancelEdit.Visibility = Visibility.Visible;

        TxtLogin.Text = user.Login;
        TxtFullName.Text = user.FullName;
        CmbRole.SelectedItem = user.Role;
        TxtPassword.Text = "";
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editingUserId = null;
        FormTitle.Text = "➕ Добавить пользователя";
        BtnCancelEdit.Visibility = Visibility.Collapsed;

        TxtLogin.Text = "";
        TxtPassword.Text = "";
        TxtFullName.Text = "";
        CmbRole.SelectedIndex = -1;
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgUsers.SelectedItem is not User selected) return;

        if (selected.Login == "admin")
        {
            MessageBox.Show("Нельзя удалить главного администратора.", "Ошибка");
            return;
        }

        if (MessageBox.Show($"Удалить пользователя '{selected.Login}'?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        try
        {
            _db.Users.Remove(selected);
            await _db.SaveChangesAsync();
            await LoadUsersAsync();
            MessageBox.Show("Пользователь удален.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }
}