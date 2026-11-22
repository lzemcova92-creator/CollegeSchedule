using Project1.Core.Enums;
using System.Text.RegularExpressions;

namespace Project1.Core.Entities;

/// <summary>Пользователь системы.</summary>
public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string FullName { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}