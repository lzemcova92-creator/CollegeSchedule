using System;

namespace Project1.Core.Entities;

/// <summary>Период производственной практики.</summary>
public class PracticePeriod
{
    public int Id { get; set; }

    /// <summary>Группа, у которой практика.</summary>
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    /// <summary>Дата начала практики.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Дата окончания практики.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Тип практики (Производственная, Учебная и т.д.).</summary>
    public string Type { get; set; } = "Производственная";

    /// <summary>Комментарий админа.</summary>
    public string? Comment { get; set; }
}