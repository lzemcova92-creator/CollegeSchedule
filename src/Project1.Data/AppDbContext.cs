using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;

namespace Project1.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Audience> Audiences => Set<Audience>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PracticePeriod> PracticePeriods => Set<PracticePeriod>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Login).IsUnique();

        mb.Entity<User>()
            .HasOne(u => u.Group).WithMany(g => g.Users)
            .HasForeignKey(u => u.GroupId).OnDelete(DeleteBehavior.SetNull);

        // Subject -> Teacher (nullable!)
        mb.Entity<Subject>()
            .HasOne(s => s.Teacher).WithMany(t => t.Subjects)
            .HasForeignKey(s => s.TeacherId).OnDelete(DeleteBehavior.SetNull);

        // ScheduleEntry -> все связи с SetNull (чтобы можно было удалять без каскада)
        mb.Entity<ScheduleEntry>()
            .HasOne(e => e.Group).WithMany(g => g.ScheduleEntries)
            .HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<ScheduleEntry>()
            .HasOne(e => e.Subject).WithMany(s => s.ScheduleEntries)
            .HasForeignKey(e => e.SubjectId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<ScheduleEntry>()
            .HasOne(e => e.Audience).WithMany(a => a.ScheduleEntries)
            .HasForeignKey(e => e.AudienceId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<ScheduleEntry>()
            .HasOne(e => e.Teacher).WithMany(t => t.ScheduleEntries)
            .HasForeignKey(e => e.TeacherId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<AuditLog>()
            .HasOne(a => a.User).WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);

        // Связь PracticePeriod -> Group
        mb.Entity<PracticePeriod>()
            .HasOne(p => p.Group)
            .WithMany(g => g.PracticePeriods)
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade); // Удалять практики при удалении группы

        base.OnModelCreating(mb);
    }
}