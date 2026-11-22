using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Project1.Core.Entities;
using Project1.Data;
using Project1.Data.Security;
using Project1.Services;
using Project1.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace Project1.App;

public partial class MainWindow : Window
{
    private User? _currentUser;
    private MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(null!);
        DataContext = _viewModel;
    }

    public MainWindow(User user, AppDbContext db) : this()
    {
        _currentUser = user;

        _viewModel = new MainViewModel(db);
        DataContext = _viewModel;

        TxtUser.Text = $"{user.FullName} ({user.Role})";

        // === НАСТРОЙКА ДОСТУПА ПО РОЛЯМ ===
        if (user.Role == Core.Enums.UserRole.Admin)
        {
            TxtFooterInfo.Text = $"Администратор: {user.FullName}";

            BtnAddLesson.Visibility = Visibility.Visible;
            BtnManageUsers.Visibility = Visibility.Visible;
            BtnReportPdf.Visibility = Visibility.Visible;
            BtnReportExcel.Visibility = Visibility.Visible;
            BtnFreeAudiences.Visibility = Visibility.Visible;
            BtnAuditLog.Visibility = Visibility.Visible;
            BtnPractice.Visibility = Visibility.Visible;
            BtnManageSubjects.Visibility = Visibility.Visible;
            BtnManageTeachers.Visibility = Visibility.Visible;
            BtnManageAudiences.Visibility = Visibility.Visible;
            BtnManageGroups.Visibility = Visibility.Visible;
            BtnBackup.Visibility = Visibility.Visible;
            BtnRestore.Visibility = Visibility.Visible;
        }
        else if (user.Role == Core.Enums.UserRole.Teacher)
        {
            TxtFooterInfo.Text = $"Преподаватель: {user.FullName}";

            BtnAddLesson.Visibility = Visibility.Collapsed;
            BtnManageUsers.Visibility = Visibility.Collapsed;
            BtnReportPdf.Visibility = Visibility.Collapsed;
            BtnReportExcel.Visibility = Visibility.Collapsed;
            BtnFreeAudiences.Visibility = Visibility.Visible;
            BtnAuditLog.Visibility = Visibility.Collapsed;
            BtnPractice.Visibility = Visibility.Collapsed;
            BtnManageSubjects.Visibility = Visibility.Collapsed;
            BtnManageTeachers.Visibility = Visibility.Collapsed;
            BtnManageAudiences.Visibility = Visibility.Collapsed;
            BtnManageGroups.Visibility = Visibility.Collapsed;
            BtnBackup.Visibility = Visibility.Collapsed;
            BtnRestore.Visibility = Visibility.Collapsed;
        }
        else // Студент (User)
        {
            TxtFooterInfo.Text = $"Студент: {user.FullName} | Группа: {user.Group?.Name ?? "Нет"}";

            BtnAddLesson.Visibility = Visibility.Collapsed;
            BtnManageUsers.Visibility = Visibility.Collapsed;
            BtnReportPdf.Visibility = Visibility.Collapsed;
            BtnReportExcel.Visibility = Visibility.Collapsed;
            BtnFreeAudiences.Visibility = Visibility.Collapsed;
            BtnAuditLog.Visibility = Visibility.Collapsed;
            BtnPractice.Visibility = Visibility.Collapsed;
            BtnManageSubjects.Visibility = Visibility.Collapsed;
            BtnManageTeachers.Visibility = Visibility.Collapsed;
            BtnManageAudiences.Visibility = Visibility.Collapsed;
            BtnManageGroups.Visibility = Visibility.Collapsed;
            BtnBackup.Visibility = Visibility.Collapsed;
            BtnRestore.Visibility = Visibility.Collapsed;
        }

        Loaded += async (s, e) => await _viewModel.InitializeAsync(user);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    // Кнопка ЗАКРЫТЬ (крестик)
    private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    // НОВАЯ Кнопка СВЕРНУТЬ (минус)
    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    // ИСПРАВЛЕННАЯ Кнопка ВЫЙТИ (возврат на авторизацию)
    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Создаем новые сервисы для окна логина
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=college.db");
            var db = new AppDbContext(optionsBuilder.Options);

            var hasher = new PasswordHasher();
            var authService = new AuthService(db, hasher);

            // Открываем окно логина
            var loginWindow = new LoginWindow(authService, db);
            loginWindow.Show();

            // Закрываем текущее главное окно
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при выходе: {ex.Message}", "Ошибка");
        }
    }

    private async Task RefreshFiltersAsync()
    {
        await _viewModel.ReloadListsAsync();
    }

    // ... (Остальные методы BtnAddLesson_Click, OpenManageUsers_Click и т.д. остаются без изменений)
    // Вставьте их сюда из предыдущего полного кода MainWindow.xaml.cs ...
    // (Чтобы не дублировать 300 строк, я даю только измененные части, 
    // но так как вы просили полный код, вот остальные методы:)

    private async void BtnAddLesson_Click(object sender, RoutedEventArgs e)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var addWindow = new AddLessonWindow(db, _currentUser?.Id);

        if (addWindow.ShowDialog() == true)
        {
            await _viewModel.LoadScheduleCommand.ExecuteAsync(null);
        }
    }

    private async void BtnReportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedGroup == null)
        {
            MessageBox.Show("Сначала выберите группу в фильтре!", "Внимание");
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"Расписание_{_viewModel.SelectedGroup.Name}.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
                var reportService = new ReportService(db);
                await reportService.GenerateSchedulePdfAsync(_viewModel.SelectedGroup.Id, saveDialog.FileName);
                MessageBox.Show("PDF-отчёт успешно сохранен!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }
    }

    private async void BtnReportExcel_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"Нагрузка_преподавателей.xlsx"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
                var reportService = new ReportService(db);
                await reportService.GenerateTeachersExcelAsync(saveDialog.FileName);
                MessageBox.Show("Excel-отчёт успешно сохранен!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }
    }

    private void OpenFreeAudiences_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=college.db");
            var db = new AppDbContext(optionsBuilder.Options);

            bool isAdmin = _currentUser?.Role == Core.Enums.UserRole.Admin;
            var window = new FreeAudiencesWindow(db, isAdmin);
            window.Owner = this;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии окна: {ex.Message}", "Ошибка");
        }
    }

    private void OpenAuditLog_Click(object sender, RoutedEventArgs e)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=college.db");
        var db = new AppDbContext(optionsBuilder.Options);

        var window = new AuditLogWindow(db);
        window.Owner = this;
        window.ShowDialog();
    }

    private async void OpenPracticeManager_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=college.db");
            var db = new AppDbContext(optionsBuilder.Options);

            var window = new ManagePracticeWindow(db);
            window.Owner = this;
            window.ShowDialog();

            await _viewModel.RefreshPracticeStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
        }
    }

    private void OpenManageUsers_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var hasher = new PasswordHasher();
        var window = new ManageUsersWindow(db, hasher);
        window.Owner = this;
        window.ShowDialog();
    }

    private async void OpenManageSubjects_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var window = new ManageSubjectsWindow(db);
        window.Owner = this;
        window.ShowDialog();
        await RefreshFiltersAsync();
    }

    private async void OpenManageTeachers_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var window = new ManageTeachersWindow(db, _currentUser?.Id);
        window.Owner = this;
        window.ShowDialog();
        await RefreshFiltersAsync();
    }

    private async void OpenManageAudiences_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var window = new ManageAudiencesWindow(db);
        window.Owner = this;
        window.ShowDialog();
        await RefreshFiltersAsync();
    }

    private async void OpenManageGroups_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != Core.Enums.UserRole.Admin) return;
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=college.db").Options);
        var window = new ManageGroupsWindow(db);
        window.Owner = this;
        window.ShowDialog();
        await RefreshFiltersAsync();
    }

    private void OpenAbout_Click(object sender, RoutedEventArgs e)
    {
        var window = new AboutWindow();
        window.Owner = this;
        window.ShowDialog();
    }

    private void BtnBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dbPath = "college.db";
            if (!System.IO.File.Exists(dbPath))
            {
                MessageBox.Show("Файл базы данных не найден!", "Ошибка");
                return;
            }

            string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!System.IO.Directory.Exists(backupDir))
            {
                System.IO.Directory.CreateDirectory(backupDir);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupPath = System.IO.Path.Combine(backupDir, $"college_backup_{timestamp}.db");

            System.IO.File.Copy(dbPath, backupPath, true);

            MessageBox.Show($"Бэкап успешно создан!\n\nПуть: {backupPath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при создании бэкапа: {ex.Message}", "Ошибка");
        }
    }

    private void BtnRestore_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "SQLite Database|*.db",
            Title = "Выберите файл бэкапа"
        };

        if (openDialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите восстановить базу данных?\n\nТекущие данные будут заменены данными из бэкапа. Приложение будет перезапущено.",
                "Подтверждение восстановления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                string dbPath = "college.db";
                System.IO.File.Copy(openDialog.FileName, dbPath, true);

                MessageBox.Show("База данных успешно восстановлена! Приложение будет перезапущено.", "Успех");

                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении: {ex.Message}\n\nУбедитесь, что приложение не блокирует файл базы данных.", "Ошибка");
            }
        }
    }
}