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

public partial class ManageAudiencesWindow : Window
{
    private readonly AppDbContext _db;
    private int? _editingAudienceId = null;

    public ManageAudiencesWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;

        Loaded += async (s, e) => await LoadAudiencesAsync();

        // Заполняем типы занятий (лекция, лаборатория)
        CmbType.ItemsSource = Enum.GetValues(typeof(LessonType));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async Task LoadAudiencesAsync()
    {
        try
        {
            var audiences = await _db.Audiences.ToListAsync();
            DgAudiences.ItemsSource = audiences;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки аудиторий: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNumber.Text))
        {
            MessageBox.Show("Введите номер аудитории.", "Внимание");
            return;
        }

        if (!int.TryParse(TxtCapacity.Text, out int capacity) || capacity <= 0)
        {
            MessageBox.Show("Введите корректную вместимость (целое число больше 0).", "Ошибка");
            return;
        }

        if (CmbType.SelectedItem is not LessonType type)
        {
            MessageBox.Show("Выберите тип аудитории.", "Внимание");
            return;
        }

        try
        {
            if (_editingAudienceId.HasValue)
            {
                var aud = await _db.Audiences.FindAsync(_editingAudienceId.Value);
                if (aud == null) return;

                aud.Number = TxtNumber.Text.Trim();
                aud.Building = TxtBuilding.Text.Trim();
                aud.Capacity = capacity;
                aud.HasProjector = ChkProjector.IsChecked == true;
                aud.Type = type;

                _db.Audiences.Update(aud);
                await _db.SaveChangesAsync();
                MessageBox.Show("Аудитория обновлена!", "Успех");
            }
            else
            {
                if (await _db.Audiences.AnyAsync(a => a.Number == TxtNumber.Text.Trim()))
                {
                    MessageBox.Show("Аудитория с таким номером уже существует.", "Ошибка");
                    return;
                }

                var newAud = new Audience
                {
                    Number = TxtNumber.Text.Trim(),
                    Building = TxtBuilding.Text.Trim(),
                    Capacity = capacity,
                    HasProjector = ChkProjector.IsChecked == true,
                    Type = type
                };

                _db.Audiences.Add(newAud);
                await _db.SaveChangesAsync();
                MessageBox.Show("Аудитория добавлена!", "Успех");
            }

            ResetForm();
            await LoadAudiencesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (DgAudiences.SelectedItem is not Audience selected) return;
        LoadAudienceToForm(selected);
    }

    private void DgAudiences_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgAudiences.SelectedItem is Audience selected) LoadAudienceToForm(selected);
    }

    private void LoadAudienceToForm(Audience aud)
    {
        _editingAudienceId = aud.Id;
        FormTitle.Text = "✏️ Редактировать аудиторию";
        BtnCancelEdit.Visibility = Visibility.Visible;

        TxtNumber.Text = aud.Number;
        TxtBuilding.Text = aud.Building;
        TxtCapacity.Text = aud.Capacity.ToString();
        ChkProjector.IsChecked = aud.HasProjector;
        CmbType.SelectedItem = aud.Type;
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editingAudienceId = null;
        FormTitle.Text = "➕ Добавить аудиторию";
        BtnCancelEdit.Visibility = Visibility.Collapsed;

        TxtNumber.Text = "";
        TxtBuilding.Text = "Главный";
        TxtCapacity.Text = "30";
        ChkProjector.IsChecked = false;
        CmbType.SelectedIndex = -1;
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgAudiences.SelectedItem is not Audience selected) return;

        // Проверяем, используется ли аудитория в расписании
        var isUsed = await _db.ScheduleEntries.AnyAsync(se => se.AudienceId == selected.Id);
        if (isUsed)
        {
            MessageBox.Show("Нельзя удалить аудиторию, так как она используется в расписании.", "Ошибка");
            return;
        }

        if (MessageBox.Show($"Удалить аудиторию '{selected.Number}'?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _db.Audiences.Remove(selected);
            await _db.SaveChangesAsync();
            await LoadAudiencesAsync();
            MessageBox.Show("Аудитория удалена.", "Успех");
            ResetForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
        }
    }
}