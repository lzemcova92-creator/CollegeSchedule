using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Enums;
using Project1.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Project1.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    private static bool _licenseSet = false;

    public ReportService(AppDbContext db)
    {
        _db = db;
        if (!_licenseSet)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            _licenseSet = true;
        }
    }

    // ==========================================
    // 1. PDF — расписание группы
    // ==========================================
    public async Task<string> GenerateSchedulePdfAsync(int groupId, string filePath)
    {
        try
        {
            var group = await _db.Groups.FindAsync(groupId);
            if (group == null) throw new Exception("Группа не найдена");

            var entries = await _db.ScheduleEntries
                .Include(e => e.Subject)
                .Include(e => e.Teacher)
                .Include(e => e.Audience)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();

            entries = entries.OrderBy(e => e.DayOfWeek).ThenBy(e => e.StartTime).ToList();

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var days = new[]
            {
                (DayOfWeek.Monday,    "Пн"),
                (DayOfWeek.Tuesday,   "Вт"),
                (DayOfWeek.Wednesday, "Ср"),
                (DayOfWeek.Thursday,  "Чт"),
                (DayOfWeek.Friday,    "Пт")
            };

            var times = entries.Select(e => e.StartTime).Distinct().OrderBy(t => t).ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text($"Расписание группы: {group.Name}")
                            .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().AlignRight().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(80);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                .AlignCenter().Text("Время").FontColor(Colors.White).Bold();
                            foreach (var (_, name) in days)
                            {
                                h.Cell().Background(Colors.Blue.Darken2).Padding(6)
                                    .AlignCenter().Text(name).FontColor(Colors.White).Bold();
                            }
                        });

                        foreach (var time in times)
                        {
                            var endTime = entries.FirstOrDefault(e => e.StartTime == time)?.EndTime
                                ?? time + TimeSpan.FromMinutes(45);

                            table.Cell().Background(Colors.Grey.Lighten3).Padding(6)
                                .AlignCenter().Text($"{time:hh\\:mm}–{endTime:hh\\:mm}")
                                .FontSize(8).Bold();

                            foreach (var (day, _) in days)
                            {
                                var lesson = entries.FirstOrDefault(e =>
                                    e.DayOfWeek == day && e.StartTime == time);

                                var color = lesson?.Subject?.Type switch
                                {
                                    LessonType.Lecture => Colors.Indigo.Lighten4,
                                    LessonType.Practice => Colors.Green.Lighten4,
                                    LessonType.Lab => Colors.Orange.Lighten4,
                                    LessonType.Curator => Colors.Pink.Lighten4,
                                    LessonType.Lunch => Colors.Grey.Lighten3,
                                    LessonType.PracticeWork => Colors.Purple.Lighten4,
                                    _ => Colors.White
                                };

                                table.Cell()
                                    .Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Background(color)
                                    .Padding(5).Column(col =>
                                    {
                                        if (lesson == null) return;

                                        col.Item().Text(lesson.Subject?.Name ?? "—")
                                            .FontSize(8).Bold();

                                        var type = lesson.Subject?.Type ?? LessonType.Lecture;
                                        if (type != LessonType.Lunch && type != LessonType.PracticeWork)
                                        {
                                            if (!string.IsNullOrEmpty(lesson.Teacher?.FullName))
                                                col.Item().Text(lesson.Teacher.FullName).FontSize(7);
                                            if (!string.IsNullOrEmpty(lesson.Audience?.Number))
                                                col.Item().Text($"Ауд. {lesson.Audience.Number}").FontSize(7);
                                        }
                                    });
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                        t.Span("Страница ");
                        t.CurrentPageNumber();
                        t.Span(" из ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);

            return filePath;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при создании PDF: " + ex.Message);
        }
    }

    // ==========================================
    // 2. Excel — нагрузка преподавателей
    // ==========================================
    public async Task<string> GenerateTeachersExcelAsync(string filePath)
    {
        try
        {
            var teachers = await _db.Teachers.ToListAsync();
            var entries = await _db.ScheduleEntries
                .Include(e => e.Subject)
                .Include(e => e.Group)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Нагрузка преподавателей");

            ws.Cell(1, 1).Value = "ФИО Преподавателя";
            ws.Cell(1, 2).Value = "Норма (ч)";
            ws.Cell(1, 3).Value = "Факт (ч)";
            ws.Cell(1, 4).Value = "Статус";

            var header = ws.Range("A1:D1");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#6366F1");
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var t in teachers)
            {
                var teacherEntries = entries
                    .Where(e => e.TeacherId == t.Id &&
                                e.Subject?.Type != LessonType.Lunch &&
                                e.Subject?.Type != LessonType.PracticeWork)
                    .ToList();

                int actualHours = teacherEntries.Count * 2;

                ws.Cell(row, 1).Value = t.FullName;
                ws.Cell(row, 2).Value = t.MaxHoursPerWeek;
                ws.Cell(row, 3).Value = actualHours;

                if (actualHours > t.MaxHoursPerWeek)
                {
                    ws.Cell(row, 4).Value = "ПЕРЕГРУЗКА";
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FCA5A5");
                }
                else
                {
                    ws.Cell(row, 4).Value = "Норма";
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#A7F3D0");
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при создании Excel: " + ex.Message);
        }
    }
}