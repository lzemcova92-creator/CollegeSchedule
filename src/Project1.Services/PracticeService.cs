using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;

namespace Project1.Services;

public class PracticeService
{
    private readonly AppDbContext _db;

    public PracticeService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Проверяет, есть ли у группы активная практика на указанную дату.</summary>
    public async Task<bool> HasActivePracticeAsync(int groupId, DateTime date)
    {
        return await _db.PracticePeriods.AnyAsync(p =>
            p.GroupId == groupId &&
            p.StartDate <= date &&
            p.EndDate >= date);
    }

    /// <summary>Возвращает период практики для группы, если он есть на сегодня.</summary>
    public async Task<PracticePeriod?> GetCurrentPracticeAsync(int groupId)
    {
        var today = DateTime.Today;
        return await _db.PracticePeriods.FirstOrDefaultAsync(p =>
            p.GroupId == groupId &&
            p.StartDate <= today &&
            p.EndDate >= today);
    }

    /// <summary>Создает новый период практики.</summary>
    public async Task CreatePracticeAsync(int groupId, DateTime start, DateTime end, string type, string? comment)
    {
        // Проверяем пересечения
        var overlaps = await _db.PracticePeriods.AnyAsync(p =>
            p.GroupId == groupId &&
            !(p.EndDate < start || p.StartDate > end));

        if (overlaps) throw new Exception("Практика уже назначена на эти даты!");

        var period = new PracticePeriod
        {
            GroupId = groupId,
            StartDate = start.Date,
            EndDate = end.Date,
            Type = type,
            Comment = comment
        };

        await _db.PracticePeriods.AddAsync(period);
        await _db.SaveChangesAsync();

        // Логируем действие
        await LogActionAsync($"Назначена практика для группы ID={groupId} с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}");
    }

    /// <summary>Обновляет существующий период практики.</summary>
    public async Task UpdatePracticeAsync(int practiceId, int groupId, DateTime start, DateTime end, string type, string? comment)
    {
        var period = await _db.PracticePeriods.FindAsync(practiceId);
        if (period == null) throw new Exception("Практика не найдена в базе данных.");

        // Проверяем пересечения (исключая текущую запись)
        var overlaps = await _db.PracticePeriods.AnyAsync(p =>
            p.Id != practiceId &&
            p.GroupId == groupId &&
            !(p.EndDate < start || p.StartDate > end));

        if (overlaps) throw new Exception("Практика уже назначена на эти даты для этой группы!");

        period.GroupId = groupId;
        period.StartDate = start.Date;
        period.EndDate = end.Date;
        period.Type = type;
        period.Comment = comment;

        _db.PracticePeriods.Update(period);
        await _db.SaveChangesAsync();

        await LogActionAsync($"Обновлена практика ID={practiceId} для группы ID={groupId} с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}");
    }

    /// <summary>Удаляет один период практики.</summary>
    public async Task RemovePracticeAsync(int practiceId)
    {
        var period = await _db.PracticePeriods.FindAsync(practiceId);
        if (period == null) return;

        _db.PracticePeriods.Remove(period);
        await _db.SaveChangesAsync();

        await LogActionAsync($"Удалена практика ID={practiceId}");
    }

    /// <summary>Удаляет все периоды практики для группы.</summary>
    public async Task RemoveAllPracticesForGroupAsync(int groupId)
    {
        var periods = await _db.PracticePeriods.Where(p => p.GroupId == groupId).ToListAsync();
        if (periods.Count > 0)
        {
            _db.PracticePeriods.RemoveRange(periods);
            await _db.SaveChangesAsync();
            await LogActionAsync($"Удалены все практики для группы ID={groupId}");
        }
    }

    /// <summary>Полная очистка всех практик в БД.</summary>
    public async Task ClearAllPracticesAsync()
    {
        var periods = await _db.PracticePeriods.ToListAsync();
        if (periods.Count > 0)
        {
            _db.PracticePeriods.RemoveRange(periods);
            await _db.SaveChangesAsync();
            await LogActionAsync("Полная очистка всех практик в БД");
        }
    }

    /// <summary>Получает все практики из базы (без фильтра по дате).</summary>
    public async Task<List<PracticePeriod>> GetAllUpcomingPracticesAsync()
    {
        return await _db.PracticePeriods
            .Include(p => p.Group)
            .OrderBy(p => p.StartDate)
            .ToListAsync();
    }

    /// <summary>Вспомогательный метод для логирования действий в AuditLog.</summary>
    private async Task LogActionAsync(string description)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = 1, // TODO: Заменить на реального пользователя из сессии
                Action = "PracticeManagement",
                Timestamp = DateTime.Now,
                Description = description // <--- Исправлено на Description
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Логирование не должно ломать основной процесс
        }
    }
}