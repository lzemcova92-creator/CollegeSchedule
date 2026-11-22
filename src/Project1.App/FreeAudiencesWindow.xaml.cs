using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;

namespace Project1.App;

public class AudienceStatus
{
    public string Number { get; set; } = "";
    public string Building { get; set; } = "";
    public int Capacity { get; set; }
    public string Status { get; set; } = "";
    public string StatusColor { get; set; } = "";
}

public partial class FreeAudiencesWindow : Window
{
    private readonly AppDbContext _db;

    // Добавили параметр isAdmin
    public FreeAudiencesWindow(AppDbContext db, bool isAdmin)
    {
        InitializeComponent();
        _db = db;

        // Показываем кнопку отладки только админам
        BtnDebug.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        CmbDay.ItemsSource = new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница" };
        CmbDay.SelectedIndex = 0;

        CmbTime.ItemsSource = new[] {
            "8:00-8:45", "8:55-9:40", "9:50-10:35", "10:45-11:30",
            "11:40-12:25", "12:35-13:20", "13:30-14:15", "14:25-15:10",
            "15:20-16:05", "16:15-17:00", "17:10-17:55", "18:05-18:50", "19:00-19:45"
        };
        CmbTime.SelectedIndex = 0;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        if (CmbDay.SelectedItem is not string dayStr || CmbTime.SelectedItem is not string timeStr)
        {
            MessageBox.Show("Выберите день и время.", "Внимание");
            return;
        }

        var times = timeStr.Replace(" ", "").Split('-');
        var startTime = TimeSpan.Parse(times[0]);
        var endTime = TimeSpan.Parse(times[1]);

        DayOfWeek day = dayStr switch
        {
            "Понедельник" => DayOfWeek.Monday,
            "Вторник" => DayOfWeek.Tuesday,
            "Среда" => DayOfWeek.Wednesday,
            "Четверг" => DayOfWeek.Thursday,
            "Пятница" => DayOfWeek.Friday,
            _ => DayOfWeek.Monday
        };

        try
        {
            var allAudiences = await _db.Audiences.ToListAsync();

            var allEntriesForDay = await _db.ScheduleEntries
                .Include(s => s.Subject)
                .Include(s => s.Group)
                .Where(s => s.DayOfWeek == day)
                .ToListAsync();

            var schedule = allEntriesForDay
                .Where(s => s.StartTime < endTime && s.EndTime > startTime)
                .ToList();

            var result = new List<AudienceStatus>();

            foreach (var aud in allAudiences)
            {
                var busyEntries = schedule.Where(s => s.AudienceId == aud.Id).ToList();

                if (busyEntries.Any())
                {
                    var statusText = string.Join("; ", busyEntries.Select(b =>
                        $"{b.Subject?.Name} ({b.Group?.Name}) [{b.StartTime:hh\\:mm}-{b.EndTime:hh\\:mm}]"));

                    result.Add(new AudienceStatus
                    {
                        Number = aud.Number,
                        Building = aud.Building,
                        Capacity = aud.Capacity,
                        Status = $"Занят: {statusText}",
                        StatusColor = "#EF4444"
                    });
                }
                else
                {
                    result.Add(new AudienceStatus
                    {
                        Number = aud.Number,
                        Building = aud.Building,
                        Capacity = aud.Capacity,
                        Status = "Свободен",
                        StatusColor = "#10B981"
                    });
                }
            }

            DgAudiences.ItemsSource = result.OrderByDescending(r => r.Status == "Свободен").ThenBy(r => r.Number).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка");
        }
    }

    private async void BtnShowAll_Click(object sender, RoutedEventArgs e)
    {
        if (CmbDay.SelectedItem is not string dayStr)
        {
            MessageBox.Show("Выберите день.", "Внимание");
            return;
        }

        DayOfWeek day = dayStr switch
        {
            "Понедельник" => DayOfWeek.Monday,
            "Вторник" => DayOfWeek.Tuesday,
            "Среда" => DayOfWeek.Wednesday,
            "Четверг" => DayOfWeek.Thursday,
            "Пятница" => DayOfWeek.Friday,
            _ => DayOfWeek.Monday
        };

        try
        {
            var allEntries = await _db.ScheduleEntries
                .Include(s => s.Subject)
                .Include(s => s.Group)
                .Include(s => s.Audience)
                .Where(s => s.DayOfWeek == day)
                .ToListAsync();

            var sortedEntries = allEntries.OrderBy(s => s.StartTime).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Все пары на {dayStr}:");

            foreach (var entry in sortedEntries)
            {
                sb.AppendLine($"{entry.StartTime:hh\\:mm} - {entry.EndTime:hh\\:mm} | {entry.Subject?.Name} | Группа: {entry.Group?.Name} | Каб: {entry.Audience?.Number}");
            }

            if (allEntries.Count == 0)
                sb.AppendLine("НЕТ ПАР ВООБЩЕ.");

            MessageBox.Show(sb.ToString(), "Сырые данные из БД", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
        }
    }
}