using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Core.Enums;
using Project1.Core.Interfaces;

namespace Project1.Data;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly Random _rnd = new(42);

    private static readonly string[] AudiencesList = { "21", "22а", "22б", "23", "24", "25", "27", "29", "31", "32а", "32б", "54", "57", "58", "61", "62", "Спортзал" };

    private static readonly (string Name, string Dept)[] MyTeachers =
    {
        ("Талыгина Ирина Геннадьевна", "Информационные технологии"),
        ("Лахно Екатерина Александровна", "Информационные технологии"),
        ("Итальянцева Виктория Михайловна", "Гуманитарные дисциплины"),
        ("Аювджи Ольга Владимировна", "Информационные технологии"),
        ("Костюкова Анна Валерьевна", "Естественные науки"),
        ("Шмарина Виктория Андреевна", "Математика"),
        ("Лахно Денис Дмитриевич", "Информационные технологии"),
        ("Левина Любовь Михайловна", "Гуманитарные дисциплины"),
        ("Томашев Владислав Алексеевич", "Информационные технологии"),
        ("Сурогатова Татьяна Васильевна", "Экономика"),
        ("Кобылинский Артем Павлович", "Информационные технологии"),
        ("Воронцова Ксения Олеговна", "Иностранные языки"),
        ("Ромашков Антон Андреевич", "Физическая культура"),
        ("Котиков Вячеслав Михайлович", "Информационные технологии"),
        ("Кузнецова Яна Александровна", "Иностранные языки")
    };

    private static readonly (string Name, int Course, string Specialty)[] MyGroups = {
        ("12о", 1, "Разработка и сопровождение ПО"),
        ("22о", 2, "Разработка и сопровождение ПО"),
        ("32о", 3, "Разработка и сопровождение ПО")
    };

    public DbSeeder(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<bool> SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(ct)) return false;
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var groups = MyGroups.Select(g => new Group { Name = g.Name, Course = g.Course, Specialty = g.Specialty }).ToList();
        await _db.Groups.AddRangeAsync(groups, ct);
        await _db.SaveChangesAsync(ct);

        var audiences = AudiencesList.Select(a => new Audience { Number = a, Building = "Главный", Capacity = 30, Type = LessonType.Lecture }).ToList();
        await _db.Audiences.AddRangeAsync(audiences, ct);
        await _db.SaveChangesAsync(ct);

        var teachers = MyTeachers.Select(t => new Teacher { FullName = t.Name, Department = t.Dept, MaxHoursPerWeek = 24 }).ToList();
        await _db.Teachers.AddRangeAsync(teachers, ct);
        await _db.SaveChangesAsync(ct);

        var users = new List<User>();

        // 1. Админы
        var (ah, aslt) = _hasher.Hash("admin123");
        users.Add(new User { Login = "admin", PasswordHash = ah, PasswordSalt = aslt, Role = UserRole.Admin, FullName = "Системный Администратор" });

        var (ah2, aslt2) = _hasher.Hash("admin123");
        users.Add(new User { Login = "ayuvdzhi", PasswordHash = ah2, PasswordSalt = aslt2, Role = UserRole.Admin, FullName = "Аювджи Ольга Владимировна" });

        // 2. Студент (Роль User) — оставляем только одного
        var (hStud, sStud) = _hasher.Hash("student123");
        users.Add(new User
        {
            Login = "student1",
            PasswordHash = hStud,
            PasswordSalt = sStud,
            Role = UserRole.User, // Студент
            FullName = "Иванов Иван Иванович", // Можно написать любое имя
            GroupId = groups[0].Id // Привязываем к первой группе (12о)
        });

        // 3. Преподаватели (Роль Teacher)
        for (int i = 0; i < teachers.Count; i++)
        {
            if (teachers[i].FullName.Contains("Аювджи")) continue;
            var (h, s) = _hasher.Hash("teacher123");
            users.Add(new User
            {
                Login = $"teacher{i + 1}",
                PasswordHash = h,
                PasswordSalt = s,
                Role = UserRole.Teacher, // Преподаватель
                FullName = teachers[i].FullName
            });
        }

        await _db.Users.AddRangeAsync(users, ct);
        await _db.SaveChangesAsync(ct);

        var g12o = groups.First(x => x.Name == "12о");
        var g22o = groups.First(x => x.Name == "22о");
        Teacher T(string name) => teachers.First(x => x.FullName.Contains(name));
        Audience A(string num) => audiences.First(x => x.Number == num);

        var subj12o = new List<Subject> {
            new() { Name = "Математика в проф. деятельности", TeacherId = T("Аювджи").Id, Type = LessonType.Lecture },
            new() { Name = "Технология разработки ПО", TeacherId = T("Сурогатова").Id, Type = LessonType.Lecture },
            new() { Name = "Кураторский час", TeacherId = T("Кузнецова").Id, Type = LessonType.Curator },
            new() { Name = "УП (Квалификация рабочего)", TeacherId = null, Type = LessonType.PracticeWork },
            new() { Name = "Физическая культура", TeacherId = T("Котиков").Id, Type = LessonType.Practice },
            new() { Name = "Охрана труда", TeacherId = T("Талыгина").Id, Type = LessonType.Lecture },
            new() { Name = "Основы алгоритмизации", TeacherId = T("Сурогатова").Id, Type = LessonType.Lecture },
            new() { Name = "Арифметико-логические основы", TeacherId = T("Томашев").Id, Type = LessonType.Lecture },
            new() { Name = "Инструментальное ПО", TeacherId = T("Кобылинский").Id, Type = LessonType.Lab }
        };

        var subj22o = new List<Subject> {
            new() { Name = "Технология разработки ПО", TeacherId = T("Сурогатова").Id, Type = LessonType.Lecture },
            new() { Name = "Кураторский час", TeacherId = T("Лахно Екатерина").Id, Type = LessonType.Curator },
            new() { Name = "Физическая культура", TeacherId = T("Котиков").Id, Type = LessonType.Practice },
            new() { Name = "УП (Разработка ПО)", TeacherId = null, Type = LessonType.PracticeWork },
            new() { Name = "Техники коммуникации", TeacherId = T("Костюкова").Id, Type = LessonType.Practice },
            new() { Name = "Основы инф. безопасности", TeacherId = T("Левина").Id, Type = LessonType.Lecture },
            new() { Name = "Программирование на С++", TeacherId = T("Ромашков").Id, Type = LessonType.Lab },
            new() { Name = "Иностранный язык (проф)", TeacherId = T("Кузнецова").Id, Type = LessonType.Practice },
            new() { Name = "Конструирование программ", TeacherId = T("Томашев").Id, Type = LessonType.Lecture }
        };

        var lunch = new Subject { Name = "ОБЕД", Type = LessonType.Lunch, TeacherId = null };

        var allSubjects = subj12o.Concat(subj22o).Append(lunch).ToList();
        await _db.Subjects.AddRangeAsync(allSubjects, ct);
        await _db.SaveChangesAsync(ct);

        var entries = new List<ScheduleEntry>();
        var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        var slots = new (TimeSpan S, TimeSpan E)[] {
            (new(8,0,0), new(8,45,0)), (new(8,55,0), new(9,40,0)),
            (new(9,50,0), new(10,35,0)), (new(10,45,0), new(11,30,0)),
            (new(11,40,0), new(12,25,0)), (new(12,35,0), new(13,20,0)),
            (new(13,30,0), new(14,15,0)), (new(14,25,0), new(15,10,0))
        };

        void AddDay(Group group, List<Subject> subs, int dayIndex, params (string subName, string? audNum)[] lessons)
        {
            var day = days[dayIndex];
            int slotIdx = 0;

            entries.Add(new ScheduleEntry
            {
                GroupId = group.Id,
                SubjectId = lunch.Id,
                TeacherId = null,
                AudienceId = null,
                DayOfWeek = day,
                StartTime = slots[2].S,
                EndTime = slots[2].E,
                WeekType = WeekType.All
            });

            foreach (var (subName, audNum) in lessons)
            {
                var sub = subs.First(x => x.Name == subName);
                int count = (sub.Type == LessonType.Curator || sub.Name.Contains("Физическая")) ? 1 : 2;
                for (int i = 0; i < count; i++)
                {
                    if (slotIdx == 2) slotIdx++;
                    if (slotIdx >= slots.Length) break;

                    int? audId = null;
                    if (audNum != null)
                    {
                        string currentAud = audNum;
                        if (audNum.Contains(","))
                        {
                            var arr = audNum.Split(',');
                            currentAud = arr[_rnd.Next(arr.Length)].Trim();
                        }
                        audId = A(currentAud).Id;
                    }

                    entries.Add(new ScheduleEntry
                    {
                        GroupId = group.Id,
                        SubjectId = sub.Id,
                        TeacherId = sub.TeacherId,
                        AudienceId = audId,
                        DayOfWeek = day,
                        StartTime = slots[slotIdx].S,
                        EndTime = slots[slotIdx].E,
                        WeekType = WeekType.All
                    });
                    slotIdx++;
                }
            }
        }

        AddDay(g12o, subj12o, 0, ("Математика в проф. деятельности", "57"), ("Технология разработки ПО", "21"), ("Охрана труда", "54"), ("Основы алгоритмизации", "21"));
        AddDay(g22o, subj22o, 0, ("Технология разработки ПО", "21"), ("Кураторский час", "23"), ("Техники коммуникации", "58"), ("Программирование на С++", "25"));
        AddDay(g12o, subj12o, 1, ("Арифметико-логические основы", "22а, 22б"), ("Инструментальное ПО", "25"), ("Физическая культура", "Спортзал"), ("УП (Квалификация рабочего)", null));
        AddDay(g22o, subj22o, 1, ("Основы инф. безопасности", "32б"), ("Конструирование программ", "22а, 22б"), ("Иностранный язык (проф)", "24"), ("УП (Разработка ПО)", null));
        AddDay(g12o, subj12o, 2, ("Технология разработки ПО", "21"), ("Кураторский час", "58"), ("Охрана труда", "54"), ("УП (Квалификация рабочего)", null));
        AddDay(g22o, subj22o, 2, ("Физическая культура", "Спортзал"), ("Технология разработки ПО", "21"), ("Программирование на С++", "25"), ("УП (Разработка ПО)", null));
        AddDay(g12o, subj12o, 3, ("Математика в проф. деятельности", "57"), ("Арифметико-логические основы", "22а, 22б"), ("Инструментальное ПО", "25"), ("Основы алгоритмизации", "21"));
        AddDay(g22o, subj22o, 3, ("Техники коммуникации", "58"), ("Основы инф. безопасности", "32б"), ("Конструирование программ", "22а, 22б"), ("Иностранный язык (проф)", "24"));
        AddDay(g12o, subj12o, 4, ("Технология разработки ПО", "21"), ("Арифметико-логические основы", "22а, 22б"), ("УП (Квалификация рабочего)", null));
        AddDay(g22o, subj22o, 4, ("Технология разработки ПО", "21"), ("Кураторский час", "23"), ("Программирование на С++", "25"));

        await _db.ScheduleEntries.AddRangeAsync(entries, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}