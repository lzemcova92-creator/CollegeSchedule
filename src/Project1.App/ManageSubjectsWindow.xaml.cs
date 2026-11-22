using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Core.Enums;
using Project1.Data;

namespace Project1.App;

public partial class ManageSubjectsWindow : Window
{
    private readonly AppDbContext _db;

    public ManageSubjectsWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;

        Loaded += async (s, e) => await LoadSubjectsAsync();

        // Заполняем типы занятий из Enum
        CmbSubjectType.ItemsSource = Enum.GetValues(typeof(LessonType));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadSubjectsAsync()
    {
        try
        {
            var subjects = await _db.Subjects.ToListAsync();
            DgSubjects.ItemsSource = subjects;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки предметов: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSubjectName.Text))
        {
            MessageBox.Show("Введите название предмета.", "Внимание");
            return;
        }

        if (CmbSubjectType.SelectedItem is not LessonType type)
        {
            MessageBox.Show("Выберите тип занятия.", "Внимание");
            return;
        }

        try
        {
            // Проверяем, нет ли уже такого предмета
            if (await _db.Subjects.AnyAsync(s => s.Name == TxtSubjectName.Text.Trim()))
            {
                MessageBox.Show("Предмет с таким названием уже существует.", "Ошибка");
                return;
            }

            var newSubject = new Subject
            {
                Name = TxtSubjectName.Text.Trim(),
                Type = type
            };

            _db.Subjects.Add(newSubject);
            await _db.SaveChangesAsync();

            MessageBox.Show("Предмет успешно добавлен!", "Успех");

            // Очищаем форму и обновляем таблицу
            TxtSubjectName.Text = "";
            CmbSubjectType.SelectedIndex = -1;
            await LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgSubjects.SelectedItem is not Subject selected) return;

        // Проверяем, используется ли предмет в расписании
        var isUsed = await _db.ScheduleEntries.AnyAsync(se => se.SubjectId == selected.Id);
        if (isUsed)
        {
            MessageBox.Show("Нельзя удалить предмет, так как он используется в расписании.", "Ошибка");
            return;
        }

        if (MessageBox.Show($"Вы уверены, что хотите удалить предмет '{selected.Name}'?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _db.Subjects.Remove(selected);
            await _db.SaveChangesAsync();
            await LoadSubjectsAsync();
            MessageBox.Show("Предмет удален.", "Успех");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }
}