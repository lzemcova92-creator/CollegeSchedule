using Project1.Core.Entities;
using Project1.Core.Interfaces;

namespace Project1.Services;

public class ConflictDetector : IConflictDetector
{
    public ConflictResult Check(ScheduleEntry candidate, IEnumerable<ScheduleEntry> existing, int? ignoreId = null)
    {
        var result = new ConflictResult();

        foreach (var entry in existing)
        {
            if (ignoreId.HasValue && entry.Id == ignoreId.Value) continue;
            if (!candidate.OverlapsWith(entry)) continue;

            // 1. Проверка аудитории
            if (candidate.AudienceId == entry.AudienceId)
                result.Conflicts.Add(new Conflict { Severity = ConflictSeverity.Error, Message = $"Аудитория {entry.Audience?.Number} уже занята.", Entity = "Audience" });

            // 2. Проверка преподавателя
            if (candidate.TeacherId == entry.TeacherId)
                result.Conflicts.Add(new Conflict { Severity = ConflictSeverity.Error, Message = $"Преподаватель {entry.Teacher?.FullName} уже ведет пару.", Entity = "Teacher" });

            // 3. Проверка группы
            if (candidate.GroupId == entry.GroupId)
                result.Conflicts.Add(new Conflict { Severity = ConflictSeverity.Warning, Message = $"Группа {entry.Group?.Name} уже имеет пару в это время.", Entity = "Group" });
        }

        return result;
    }
}