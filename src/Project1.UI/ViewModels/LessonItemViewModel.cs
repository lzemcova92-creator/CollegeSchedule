using System;
using Project1.Core.Entities;

namespace Project1.UI.ViewModels;

public class LessonItemViewModel
{
    public int Id { get; set; }
    public string TimeDisplay { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string AudienceNumber { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string BlockColor { get; set; } = "#334155";
    public string ToolTipText { get; set; } = string.Empty;

    // Индекс строки в сетке (0 - первая пара, 1 - вторая и т.д.)
    public int SlotIndex { get; set; }

    public LessonItemViewModel(ScheduleEntry entry)
    {
        Id = entry.Id;

        // Вычисляем индекс строки (0 для 8:00, 1 для 8:55 и т.д.)
        SlotIndex = entry.StartTime.Hours switch
        {
            8 => 0,  // 1-я пара (строка 0 в ItemsControl)
            9 => 1,  // 2-я пара
            10 => 2, // 3-я пара
            11 => 3, // 4-я пара
            12 => 4, // 5-я пара
            13 => 5, // 6-я пара
            14 => 6, // 7-я пара
            15 => 7, // 8-я пара
            16 => 8, // 9-я пара
            17 => 9, // 10-я пара
            18 => 10,// 11-я пара
            _ => 0
        };

        TimeDisplay = $"{entry.StartTime:hh\\:mm} - {entry.EndTime:hh\\:mm}";
        SubjectName = entry.Subject?.Name ?? "Неизвестно";

        AudienceNumber = !string.IsNullOrEmpty(entry.Audience?.Number) ? $"каб. {entry.Audience.Number}" : "";
        TeacherName = entry.Teacher?.FullName ?? "—";

        ToolTipText = $"{SubjectName}\nПреподаватель: {TeacherName}\nАудитория: {AudienceNumber}";

        if (SubjectName.Contains("ОБЕД", StringComparison.OrdinalIgnoreCase))
        {
            BlockColor = "#334155"; // Темно-серый
            return;
        }

        // Чередование оттенков синего (темный / светлый)
        if (SlotIndex % 2 == 0)
        {
            BlockColor = "#3B82F6"; // Яркий/светлый синий
        }
        else
        {
            BlockColor = "#2563EB"; // Более темный синий
        }
    }
}