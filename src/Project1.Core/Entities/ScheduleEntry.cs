using Project1.Core.Enums;

namespace Project1.Core.Entities;

/// <summary>Конкретная пара в расписании.</summary>
public class ScheduleEntry
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    /// <summary>Аудитория (может быть null для обеда/практики).</summary>
    public int? AudienceId { get; set; }
    public Audience? Audience { get; set; }

    /// <summary>Преподаватель (может быть null для обеда/практики).</summary>
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public WeekType WeekType { get; set; } = WeekType.All;

    public bool IsValidInterval => StartTime < EndTime;

    public bool OverlapsWith(ScheduleEntry other)
    {
        if (DayOfWeek != other.DayOfWeek) return false;
        if (!WeeksOverlap(WeekType, other.WeekType)) return false;
        return StartTime < other.EndTime && other.StartTime < EndTime;
    }

    private static bool WeeksOverlap(WeekType a, WeekType b)
    {
        if (a == WeekType.All || b == WeekType.All) return true;
        return a == b;
    }
}