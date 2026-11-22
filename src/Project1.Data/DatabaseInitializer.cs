using Microsoft.EntityFrameworkCore;
using Project1.Core.Interfaces;

namespace Project1.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(string dbPath, IPasswordHasher hasher, CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync(ct);
        var seeder = new DbSeeder(db, hasher);
        await seeder.SeedAsync(ct);
    }
}