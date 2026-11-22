using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;

namespace Project1.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Записывает действие в журнал.
    /// </summary>
    /// <param name="userId">ID пользователя, совершившего действие (может быть null для системы).</param>
    /// <param name="action">Тип действия (например, "AddLesson", "Login", "Backup").</param>
    /// <param name="description">Подробное описание действия.</param>
    public async Task LogAsync(int? userId, string action, string description)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.Now,
                Description = description
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Логирование не должно прерывать основной процесс приложения
            Console.WriteLine($"Ошибка записи в AuditLog: {ex.Message}");
        }
    }

    /// <summary>
    /// Получает последние N записей из журнала (для отображения в окне).
    /// </summary>
    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _db.AuditLogs
            .Include(l => l.User) // Подтягиваем данные пользователя для отображения ФИО
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Полностью очищает журнал действий.
    /// </summary>
    public async Task ClearJournalAsync()
    {
        var logs = await _db.AuditLogs.ToListAsync();
        if (logs.Any())
        {
            _db.AuditLogs.RemoveRange(logs);
            await _db.SaveChangesAsync();
        }
    }
}