using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;
using Project1.Services; // <-- Добавили для AuditService

namespace Project1.App;

public partial class ManageTeachersWindow : Window
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService; // <-- Сервис логирования
    private readonly int? _currentUserId; // <-- ID админа, который открыл окно
    private int? _editingTeacherId = null;

    // Теперь конструктор принимает ID пользователя
    public ManageTeachersWindow(AppDbContext db, int? currentUserId)
    {
        InitializeComponent();
        _db = db;
        _currentUserId = currentUserId;
        _auditService = new AuditService(_db);

        Loaded += async (s, e) => await LoadTeachersAsync();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadTeachersAsync()
    {
        try
        {
            var teachers = await _db.Teachers.ToListAsync();
            DgTeachers.ItemsSource = teachers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки преподавателей: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtFullName.Text))
        {
            MessageBox.Show("Введите ФИО преподавателя.", "Внимание");
            return;
        }

        if (!int.TryParse(TxtMaxHours.Text, out int maxHours) || maxHours <= 0)
        {
            MessageBox.Show("Введите корректное количество часов (целое число больше 0).", "Ошибка");
            return;
        }

        try
        {
            if (_editingTeacherId.HasValue)
            {
                var teacher = await _db.Teachers.FindAsync(_editingTeacherId.Value);
                if (teacher == null) return;

                teacher.FullName = TxtFullName.Text.Trim();
                teacher.MaxHoursPerWeek = maxHours;

                _db.Teachers.Update(teacher);
                await _db.SaveChangesAsync();

                // ЛОГИРОВАНИЕ
                await _auditService.LogAsync(_currentUserId, "EditTeacher", $"Изменен преподаватель: {teacher.FullName}");

                MessageBox.Show("Преподаватель обновлен!", "Успех");
            }
            else
            {
                if (await _db.Teachers.AnyAsync(t => t.FullName == TxtFullName.Text.Trim()))
                {
                    MessageBox.Show("Преподаватель с таким ФИО уже существует.", "Ошибка");
                    return;
                }

                var newTeacher = new Teacher
                {
                    FullName = TxtFullName.Text.Trim(),
                    Department = "",
                    MaxHoursPerWeek = maxHours
                };

                _db.Teachers.Add(newTeacher);
                await _db.SaveChangesAsync();

                // ЛОГИРОВАНИЕ
                await _auditService.LogAsync(_currentUserId, "AddTeacher", $"Добавлен преподаватель: {newTeacher.FullName}");

                MessageBox.Show("Преподаватель добавлен!", "Успех");
            }

            ResetForm();
            await LoadTeachersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgTeachers.SelectedItem is not Teacher selected) return;
        LoadTeacherToForm(selected);
    }

    private void DgTeachers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgTeachers.SelectedItem is Teacher selected) LoadTeacherToForm(selected);
    }

    private void LoadTeacherToForm(Teacher teacher)
    {
        _editingTeacherId = teacher.Id;
        FormTitle.Text = "✏️ Редактировать преподавателя";
        BtnCancelEdit.Visibility = Visibility.Visible;

        TxtFullName.Text = teacher.FullName;
        TxtMaxHours.Text = teacher.MaxHoursPerWeek.ToString();
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editingTeacherId = null;
        FormTitle.Text = "➕ Добавить преподавателя";
        BtnCancelEdit.Visibility = Visibility.Collapsed;

        TxtFullName.Text = "";
        TxtMaxHours.Text = "24";
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgTeachers.SelectedItem is not Teacher selected) return;

        var hasLessons = await _db.ScheduleEntries.AnyAsync(se => se.TeacherId == selected.Id);
        if (hasLessons)
        {
            MessageBox.Show("Нельзя удалить преподавателя, так как у него есть пары в расписании.", "Ошибка");
            return;
        }

        if (MessageBox.Show($"Удалить преподавателя '{selected.FullName}'?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _db.Teachers.Remove(selected);
            await _db.SaveChangesAsync();

            // ЛОГИРОВАНИЕ
            await _auditService.LogAsync(_currentUserId, "DeleteTeacher", $"Удален преподаватель: {selected.FullName}");

            await LoadTeachersAsync();
            MessageBox.Show("Преподаватель удален.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }
}