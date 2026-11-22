using System;

namespace Project1.Core.Entities;

/// <summary>Запись в журнале действий.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>ID пользователя, который совершил действие (может быть null, если система).</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Тип действия: AddLesson, DeleteLesson, Login, BackupCreated...</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Подробное описание (например: "Добавил пару Математика в каб. 57").</summary>
    public string Description { get; set; } = string.Empty;
}