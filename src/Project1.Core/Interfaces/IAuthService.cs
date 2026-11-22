using Project1.Core.Entities;
using Project1.Core.Enums;

namespace Project1.Core.Interfaces;

/// <summary>Результат попытки входа.</summary>
public class LoginResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public User? User { get; init; }

    public static LoginResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
    public static LoginResult Ok(User u) => new() { Success = true, User = u };
}

/// <summary>Сервис аутентификации.</summary>
public interface IAuthService
{
    /// <summary>Вход по логину и паролю.</summary>
    Task<LoginResult> LoginAsync(string login, string password, CancellationToken ct = default);

    /// <summary>Создание нового пользователя (хэширование пароля внутри).</summary>
    Task<User> CreateUserAsync(string login, string password, string fullName, UserRole role, int? groupId, CancellationToken ct = default);
}