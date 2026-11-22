using Project1.Core.Enums;

namespace Project1.Core.Entities;

/// <summary>Учебный предмет.</summary>
public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Преподаватель (может быть null для обеда/практики).</summary>
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public int HoursPerWeek { get; set; }
    public LessonType Type { get; set; } = LessonType.Lecture;
    public ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
}