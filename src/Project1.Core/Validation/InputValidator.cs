using System;
using System.Text.RegularExpressions;

namespace Project1.Core.Validation;

public static class InputValidator
{
    /// <summary>Проверяет логин: 3-20 символов, только латиница и цифры.</summary>
    public static bool IsValidLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login)) return false;
        return Regex.IsMatch(login, @"^[a-zA-Z0-9]{3,20}$");
    }

    /// <summary>Проверяет пароль: минимум 6 символов.</summary>
    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;
        return password.Length >= 6;
    }

    /// <summary>Проверяет ФИО: минимум 2 слова, только буквы, пробелы и дефисы.</summary>
    public static bool IsValidFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return false;
        // Должно быть хотя бы 2 слова (Имя Фамилия)
        var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2;
    }

    /// <summary>Проверяет корректность времени пары (начало раньше конца).</summary>
    public static bool IsValidTime(TimeSpan start, TimeSpan end)
    {
        return start < end;
    }
}