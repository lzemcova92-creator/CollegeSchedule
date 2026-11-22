using Microsoft.EntityFrameworkCore;
using Project1.Data;
using Project1.Data.Security;
using Project1.Services;
using System;
using System.IO;
using System.Windows;

namespace Project1.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 1. Путь к базе данных
            string dbPath = "college.db";

            // Удаляем старую базу для чистого старта (если нужно)
            if (File.Exists(dbPath)) File.Delete(dbPath);

            // 2. Настройка контекста EF Core
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            using (var db = new AppDbContext(optionsBuilder.Options))
            {
                // Создаем таблицы
                await db.Database.EnsureCreatedAsync();

                // Заполняем данными (Seeder)
                var hasher = new PasswordHasher();
                var seeder = new DbSeeder(db, hasher);
                await seeder.SeedAsync();
            }

            // 3. Создание сервисов для UI
            var finalDb = new AppDbContext(optionsBuilder.Options);
            var authHasher = new PasswordHasher();
            var authService = new AuthService(finalDb, authHasher);

            // 4. ОТКРЫВАЕМ ОКНО ЛОГИНА ВРУЧНУЮ
            // Теперь мы сами контролируем создание объекта LoginWindow
            var loginWindow = new LoginWindow(authService, finalDb);

            MainWindow = loginWindow; // Устанавливаем как главное окно приложения
            loginWindow.Show();       // Показываем его
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка запуска:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}