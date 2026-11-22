using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;

namespace Project1.App;

public partial class ManageGroupsWindow : Window
{
    private readonly AppDbContext _db;
    private int? _editingGroupId = null;

    public ManageGroupsWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;

        Loaded += async (s, e) => await LoadGroupsAsync();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _db.Groups.ToListAsync();
            DgGroups.ItemsSource = groups;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки групп: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("Введите название группы.", "Внимание");
            return;
        }

        if (!int.TryParse(TxtCourse.Text, out int course) || course <= 0 || course > 5)
        {
            MessageBox.Show("Введите корректный курс (от 1 до 5).", "Ошибка");
            return;
        }

        try
        {
            if (_editingGroupId.HasValue)
            {
                var group = await _db.Groups.FindAsync(_editingGroupId.Value);
                if (group == null) return;

                group.Name = TxtName.Text.Trim();
                group.Course = course;
                group.Specialty = TxtSpecialty.Text.Trim();

                _db.Groups.Update(group);
                await _db.SaveChangesAsync();
                MessageBox.Show("Группа обновлена!", "Успех");
            }
            else
            {
                if (await _db.Groups.AnyAsync(g => g.Name == TxtName.Text.Trim()))
                {
                    MessageBox.Show("Группа с таким названием уже существует.", "Ошибка");
                    return;
                }

                var newGroup = new Group
                {
                    Name = TxtName.Text.Trim(),
                    Course = course,
                    Specialty = TxtSpecialty.Text.Trim()
                };

                _db.Groups.Add(newGroup);
                await _db.SaveChangesAsync();
                MessageBox.Show("Группа добавлена!", "Успех");
            }

            ResetForm();
            await LoadGroupsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgGroups.SelectedItem is not Group selected) return;
        LoadGroupToForm(selected);
    }

    private void DgGroups_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgGroups.SelectedItem is Group selected) LoadGroupToForm(selected);
    }

    private void LoadGroupToForm(Group group)
    {
        _editingGroupId = group.Id;
        FormTitle.Text = "✏️ Редактировать группу";
        BtnCancelEdit.Visibility = Visibility.Visible;

        TxtName.Text = group.Name;
        TxtCourse.Text = group.Course.ToString();
        TxtSpecialty.Text = group.Specialty;
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editingGroupId = null;
        FormTitle.Text = "➕ Добавить группу";
        BtnCancelEdit.Visibility = Visibility.Collapsed;

        TxtName.Text = "";
        TxtCourse.Text = "1";
        TxtSpecialty.Text = "";
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgGroups.SelectedItem is not Group selected) return;

        // Проверяем, есть ли у группы пары или студенты
        var hasLessons = await _db.ScheduleEntries.AnyAsync(se => se.GroupId == selected.Id);
        var hasStudents = await _db.Users.AnyAsync(u => u.GroupId == selected.Id);
        var hasPractice = await _db.PracticePeriods.AnyAsync(p => p.GroupId == selected.Id);

        if (hasLessons || hasStudents || hasPractice)
        {
            MessageBox.Show("Нельзя удалить группу, так как у неё есть расписание, студенты или практика.", "Ошибка");
            return;
        }

        if (MessageBox.Show($"Удалить группу '{selected.Name}'?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _db.Groups.Remove(selected);
            await _db.SaveChangesAsync();
            await LoadGroupsAsync();
            MessageBox.Show("Группа удалена.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }
}