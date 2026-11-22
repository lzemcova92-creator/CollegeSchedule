using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Project1.Data;
using Project1.Services;

namespace Project1.App;

public partial class AuditLogWindow : Window
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;

    // Принимаем контекст базы данных из главного окна
    public AuditLogWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;
        _auditService = new AuditService(_db);

        // Загружаем логи при открытии
        Loaded += async (s, e) => await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        try
        {
            var logs = await _auditService.GetRecentLogsAsync(100);
            DgLogs.ItemsSource = logs;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки журнала: {ex.Message}", "Ошибка");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadLogsAsync();
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Вы уверены, что хотите удалить всю историю действий?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            try
            {
                await _auditService.ClearJournalAsync();
                await LoadLogsAsync();
                MessageBox.Show("Журнал очищен.", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки: {ex.Message}", "Ошибка");
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
    }
}