using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Data;

namespace Project1.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _db;
    private User? _currentUser;
    private List<ScheduleEntry> _allEntries = new();

    [ObservableProperty] private ObservableCollection<LessonItemViewModel> mondayLessons = new();
    [ObservableProperty] private ObservableCollection<LessonItemViewModel> tuesdayLessons = new();
    [ObservableProperty] private ObservableCollection<LessonItemViewModel> wednesdayLessons = new();
    [ObservableProperty] private ObservableCollection<LessonItemViewModel> thursdayLessons = new();
    [ObservableProperty] private ObservableCollection<LessonItemViewModel> fridayLessons = new();

    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<Teacher> Teachers { get; } = new();
    [ObservableProperty] private Group? selectedGroup;
    [ObservableProperty] private Teacher? selectedTeacher;

    [ObservableProperty] private string breadcrumbs = "Главная / Расписание";

    [ObservableProperty] private bool isPracticeMode = false;
    [ObservableProperty] private string currentPracticeHeader = "";
    [ObservableProperty] private string currentPracticeInfo = "";

    [ObservableProperty] private bool isAdmin = false;
    [ObservableProperty] private bool isLoading = false; // Свойство для спиннера

    public MainViewModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task InitializeAsync(User user)
    {
        _currentUser = user;
        IsAdmin = user.Role == Core.Enums.UserRole.Admin;

        await ReloadListsAsync();

        if (user.Role == Core.Enums.UserRole.User && user.GroupId.HasValue)
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == user.GroupId.Value);
        else
            SelectedGroup = Groups.FirstOrDefault(g => g.Name == "12о");

        await LoadScheduleAsync();
    }

    public async Task ReloadListsAsync()
    {
        Groups.Clear();
        foreach (var g in await _db.Groups.ToListAsync()) Groups.Add(g);

        Teachers.Clear();
        foreach (var t in await _db.Teachers.ToListAsync()) Teachers.Add(t);
    }

    [RelayCommand]
    public async Task LoadScheduleAsync()
    {
        IsLoading = true;
        try
        {
            _allEntries = await _db.ScheduleEntries
                .Include(e => e.Subject).ThenInclude(s => s.Teacher)
                .Include(e => e.Teacher)
                .Include(e => e.Audience)
                .Include(e => e.Group)
                .ToListAsync();

            await ApplyFiltersAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ResetFilters()
    {
        SelectedGroup = Groups.FirstOrDefault(g => g.Name == "12о");
        SelectedTeacher = null;
    }

    [RelayCommand]
    public async Task RequestDeleteLessonAsync(LessonItemViewModel? lesson)
    {
        if (lesson == null || !IsAdmin) return;

        var result = System.Windows.MessageBox.Show(
            $"Вы уверены, что хотите удалить пару?\n\nПредмет: {lesson.SubjectName}\nВремя: {lesson.TimeDisplay}",
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await DeleteLessonAsync(lesson);
        }
    }

    private async Task DeleteLessonAsync(LessonItemViewModel lesson)
    {
        var entry = await _db.ScheduleEntries.FindAsync(lesson.Id);
        if (entry != null)
        {
            _db.ScheduleEntries.Remove(entry);
            await _db.SaveChangesAsync();
            await LoadScheduleAsync();
        }
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        UpdateBreadcrumbs();
        _ = ApplyFiltersAsync();
    }

    partial void OnSelectedTeacherChanged(Teacher? value)
    {
        UpdateBreadcrumbs();
        _ = ApplyFiltersAsync();
    }

    private void UpdateBreadcrumbs()
    {
        string groupPart = SelectedGroup != null ? $" / {SelectedGroup.Name}" : "";
        string teacherPart = SelectedTeacher != null ? $" / {SelectedTeacher.FullName}" : "";
        Breadcrumbs = $"Главная / Расписание{groupPart}{teacherPart}";
    }

    public async Task RefreshPracticeStatusAsync()
    {
        await ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        if (SelectedGroup != null)
        {
            await CheckAndSetPracticeModeAsync(SelectedGroup.Id);
            if (IsPracticeMode)
            {
                MondayLessons.Clear();
                TuesdayLessons.Clear();
                WednesdayLessons.Clear();
                ThursdayLessons.Clear();
                FridayLessons.Clear();
                return;
            }
        }
        else
        {
            IsPracticeMode = false;
            CurrentPracticeHeader = "";
            CurrentPracticeInfo = "";
        }

        var filtered = _allEntries.AsEnumerable();

        if (SelectedGroup != null)
            filtered = filtered.Where(e => e.GroupId == SelectedGroup.Id);

        if (SelectedTeacher != null)
            filtered = filtered.Where(e => e.TeacherId == SelectedTeacher.Id);

        var list = filtered.OrderBy(e => e.StartTime).ToList();

        MondayLessons = new ObservableCollection<LessonItemViewModel>(
            list.Where(e => e.DayOfWeek == DayOfWeek.Monday).Select(e => new LessonItemViewModel(e)));
        TuesdayLessons = new ObservableCollection<LessonItemViewModel>(
            list.Where(e => e.DayOfWeek == DayOfWeek.Tuesday).Select(e => new LessonItemViewModel(e)));
        WednesdayLessons = new ObservableCollection<LessonItemViewModel>(
            list.Where(e => e.DayOfWeek == DayOfWeek.Wednesday).Select(e => new LessonItemViewModel(e)));
        ThursdayLessons = new ObservableCollection<LessonItemViewModel>(
            list.Where(e => e.DayOfWeek == DayOfWeek.Thursday).Select(e => new LessonItemViewModel(e)));
        FridayLessons = new ObservableCollection<LessonItemViewModel>(
            list.Where(e => e.DayOfWeek == DayOfWeek.Friday).Select(e => new LessonItemViewModel(e)));
    }

    private async Task CheckAndSetPracticeModeAsync(int groupId)
    {
        try
        {
            var activePractice = await _db.PracticePeriods
                .Include(p => p.Group)
                .FirstOrDefaultAsync(p => p.GroupId == groupId);

            if (activePractice != null)
            {
                IsPracticeMode = true;
                string dateRange = $"{activePractice.StartDate:dd.MM.yyyy} – {activePractice.EndDate:dd.MM.yyyy}";
                string typeUpper = activePractice.Type.ToUpper();
                CurrentPracticeHeader = $"🎓 {typeUpper} ПРАКТИКА";
                CurrentPracticeInfo = $"Группа: {activePractice.Group?.Name}\n" +
                                      $"📅 {dateRange}" +
                                      (!string.IsNullOrEmpty(activePractice.Comment) ? $"\n💬 {activePractice.Comment}" : "");
            }
            else
            {
                IsPracticeMode = false;
                CurrentPracticeHeader = "";
                CurrentPracticeInfo = "";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка проверки практики: {ex.Message}");
            IsPracticeMode = false;
            CurrentPracticeHeader = "";
            CurrentPracticeInfo = "";
        }
    }
}