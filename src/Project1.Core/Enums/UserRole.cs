namespace Project1.Core.Enums;

public enum UserRole
{
    Admin,   // Администратор (все права)
    Teacher, // Преподаватель (расписание + кабинеты)
    User     // Студент (только расписание)
}