using Project1.Core.Entities;

namespace Project1.Core.Interfaces;

/// <summary>Сервис журнала действий (AuditLog).</summary>
public interface IAuditService
{
    /// <summary>Записать действие в журнал.</summary>
    Task LogAsync(int? userId, string action, string details, CancellationToken ct = default);

    /// <summary>Получить последние записи журнала.</summary>
    Task<IReadOnlyList<AuditLog>> GetLogsAsync(int take = 200, CancellationToken ct = default);
}