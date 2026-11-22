using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Core.Enums;
using Project1.Core.Interfaces;
using Project1.Data;
using Project1.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Project1.App;

public partial class AddLessonWindow : Window
{
    private readonly AppDbContext _db;
    private readonly IConflictDetector _conflictDetector = new ConflictDetector();
    private readonly int? _currentUserId;
    private readonly AuditService _auditService;

    public AddLessonWindow(AppDbContext db, int? currentUserId)
    {
        InitializeComponent();
        _db = db;
        _currentUserId = currentUserId;
        _auditService = new AuditService(_db);

        Loaded += AddLessonWindow_Loaded;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private async void AddLessonWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CmbGroup.ItemsSource = await _db.Groups.ToListAsync();
        CmbSubject.ItemsSource = await _db.Subjects.Where(s => s.Type != LessonType.Lunch).ToListAsync();
        CmbTeacher.ItemsSource = await _db.Teachers.ToListAsync();
        CmbAudience.ItemsSource = await _db.Audiences.ToListAsync();

        CmbDay.ItemsSource = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница" };
        CmbTime.ItemsSource = new[] {
            "8:00-8:45", "8:55-9:40", "9:50-10:35", "10:45-11:30",
            "11:40-12:25", "12:35-13:20", "13:30-14:15", "14:25-15:10",
            "15:20-16:05", "16:15-17:00", "17:10-17:55", "18:05-18:50", "19:00-19:45"
        };
    }

    // --- ЛОГИКА ЧЕКБОКСА "ОБЕД" ---
    private void ChkLunch_Checked(object sender, RoutedEventArgs e)
    {
        // Скрываем ненужные поля
        SubjectBlock.Visibility = Visibility.Collapsed;
        TeacherBlock.Visibility = Visibility.Collapsed;
        AudienceBlock.Visibility = Visibility.Collapsed;
    }

    private void ChkLunch_Unchecked(object sender, RoutedEventArgs e)
    {
        // Показываем обратно
        SubjectBlock.Visibility = Visibility.Visible;
        TeacherBlock.Visibility = Visibility.Visible;
        AudienceBlock.Visibility = Visibility.Visible;
    }
    // ------------------------------

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        ErrorBorder.Visibility = Visibility.Collapsed;

        if (CmbGroup.SelectedItem is not Group group ||
            CmbDay.SelectedItem is not string dayStr ||
            CmbTime.SelectedItem is not string timeStr)
        {
            ShowError("Пожалуйста, заполните группу, день и время.");
            return;
        }

        var times = timeStr.Split('-');
        if (times.Length != 2)
        {
            ShowError("Неверный формат времени.");
            return;
        }

        var startTime = TimeSpan.Parse(times[0]);
        var endTime = TimeSpan.Parse(times[1]);

        // ВАЛИДАЦИЯ ВРЕМЕНИ
        if (!Project1.Core.Validation.InputValidator.IsValidTime(startTime, endTime))
        {
            ShowError("Время начала не может быть позже времени окончания!");
            return;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void ShowError(string msg)
    {
        TxtError.Text = msg;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}