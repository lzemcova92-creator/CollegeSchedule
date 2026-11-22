using Project1.Core.Enums;

namespace Project1.Core.Entities;

public class Audience
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool HasProjector { get; set; }
    public LessonType Type { get; set; } = LessonType.Lecture;
    public ICollection<ScheduleEntry> ScheduleEntries { get; set; } = new List<ScheduleEntry>();
}