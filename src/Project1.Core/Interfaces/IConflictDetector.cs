using Project1.Core.Entities;

namespace Project1.Core.Interfaces;

public enum ConflictSeverity { Warning, Error }

public class Conflict
{
    public ConflictSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
}

public class ConflictResult
{
    public List<Conflict> Conflicts { get; } = new();
    public bool HasConflict => Conflicts.Count > 0;
}

public interface IConflictDetector
{
    ConflictResult Check(ScheduleEntry candidate, IEnumerable<ScheduleEntry> existing, int? ignoreId = null);
}