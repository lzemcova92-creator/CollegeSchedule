using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;
using Project1.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project1.App;

public partial class ManagePracticeWindow : Window
{

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
    private readonly AppDbContext? _db;
    private readonly PracticeService? _practiceService;
    private int? _editingPracticeId = null;

    public ManagePracticeWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;
        _practiceService = new PracticeService(_db);

        Loaded += async (s, e) => await SafeLoadDataAsync();
    }

    private async Task SafeLoadDataAsync()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            if (!canConnect)
                throw new Exception("Не удалось подключиться к базе данных college.db");

            var groups = await _db.Groups.ToListAsync();

            if (groups.Count == 0)
                MessageBox.Show("Внимание: В базе нет ни одной группы!", "Предупреждение");

            CmbGroup.ItemsSource = groups;

            var practices = await _practiceService.GetAllUpcomingPracticesAsync();
            DgPractices.ItemsSource = practices;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных:\n{ex.Message}\n\n{ex.InnerException?.Message ?? ""}", "Ошибка данных");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CmbGroup.SelectedItem is not Group group)
            {
                MessageBox.Show("Выберите группу из списка!", "Внимание");
                return;
            }

            // Парсим даты из текстовых полей
            if (!DateTime.TryParseExact(TxtStartDate.Text.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start))
            {
                MessageBox.Show("Неверный формат даты начала! Используйте дд.мм.гггг (например, 15.11.2026)", "Ошибка");
                return;
            }

            if (!DateTime.TryParseExact(TxtEndDate.Text.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            {
                MessageBox.Show("Неверный формат даты окончания! Используйте дд.мм.гггг (например, 15.12.2026)", "Ошибка");
                return;
            }

            if (end < start)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала!", "Ошибка");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtType.Text))
            {
                MessageBox.Show("Укажите тип практики!", "Внимание");
                return;
            }

            if (_editingPracticeId.HasValue)
            {
                await _practiceService.UpdatePracticeAsync(_editingPracticeId.Value, group.Id, start, end, TxtType.Text, TxtComment.Text);
                MessageBox.Show("Практика успешно обновлена!", "Успех");
                ResetForm();
            }
            else
            {
                await _practiceService.CreatePracticeAsync(group.Id, start, end, TxtType.Text, TxtComment.Text);
                MessageBox.Show("Практика успешно назначена!", "Успех");
                ResetForm();
            }

            await SafeLoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить практику.\n\nПричина: {ex.Message}\n\n{ex.InnerException?.Message ?? ""}", "Ошибка сохранения");
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgPractices.SelectedItem is not PracticePeriod selected)
        {
            MessageBox.Show("Выберите практику в таблице для редактирования!", "Внимание");
            return;
        }

        LoadPracticeToForm(selected);
    }

    private void DgPractices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DgPractices.SelectedItem is PracticePeriod selected)
        {
            LoadPracticeToForm(selected);
        }
    }

    private void LoadPracticeToForm(PracticePeriod practice)
    {
        _editingPracticeId = practice.Id;
        FormTitle.Text = "✏️ Редактировать практику";
        BtnCancelEdit.Visibility = Visibility.Visible;

        CmbGroup.SelectedValue = practice.GroupId;
        TxtStartDate.Text = practice.StartDate.ToString("dd.MM.yyyy");
        TxtEndDate.Text = practice.EndDate.ToString("dd.MM.yyyy");
        TxtType.Text = practice.Type;
        TxtComment.Text = practice.Comment;
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ResetForm();
    }

    private void ResetForm()
    {
        _editingPracticeId = null;
        FormTitle.Text = "➕ Назначить практику";
        BtnCancelEdit.Visibility = Visibility.Collapsed;

        CmbGroup.SelectedIndex = -1;
        TxtStartDate.Text = "01.01.2026";
        TxtEndDate.Text = "01.02.2026";
        TxtType.Text = "Производственная";
        TxtComment.Text = "";
    }

    private async void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (DgPractices.SelectedItem is not PracticePeriod selected)
        {
            MessageBox.Show("Выберите практику в таблице для удаления!", "Внимание");
            return;
        }

        var result = MessageBox.Show($"Удалить практику для группы '{selected.Group?.Name}' с {selected.StartDate:dd.MM.yyyy} по {selected.EndDate:dd.MM.yyyy}?",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _practiceService.RemovePracticeAsync(selected.Id);
            await SafeLoadDataAsync();
            MessageBox.Show("Практика удалена.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnClearAll_Click(object sender, RoutedEventArgs e)
    {
        var result1 = MessageBox.Show("Вы уверены, что хотите удалить ВСЕ практики из базы данных? Это действие необратимо!",
            "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result1 != MessageBoxResult.Yes) return;

        var result2 = MessageBox.Show("Точно удалить все практики? Данные будут потеряны навсегда.",
            "Последнее подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Stop);

        if (result2 != MessageBoxResult.Yes) return;

        try
        {
            await _practiceService.ClearAllPracticesAsync();
            await SafeLoadDataAsync();
            MessageBox.Show("Все практики удалены из базы данных.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка очистки: {ex.Message}", "Ошибка");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}