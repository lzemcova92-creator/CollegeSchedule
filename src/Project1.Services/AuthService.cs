using Microsoft.EntityFrameworkCore;
using Project1.Core.Entities;
using Project1.Core.Interfaces;
using Project1.Data;

namespace Project1.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public AuthService(AppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<LoginResult> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        // Ищем пользователя по логину (без учета регистра)
        var user = await _db.Users
            .Include(u => u.Group)
            .FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower(), ct);

        if (user == null)
            return LoginResult.Fail("Неверный логин или пароль."); // По ТЗ не раскрываем детали

        if (!_hasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return LoginResult.Fail("Неверный логин или пароль.");

        return LoginResult.Ok(user);
    }

    public async Task<User> CreateUserAsync(string login, string password, string fullName, Core.Enums.UserRole role, int? groupId, CancellationToken ct = default)
    {
        var (hash, salt) = _hasher.Hash(password);
        var user = new User
        {
            Login = login,
            PasswordHash = hash,
            PasswordSalt = salt,
            FullName = fullName,
            Role = role,
            GroupId = groupId
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }
}