using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuanLyVatTu;

public static class UserStore
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "users.json");
    private static readonly object locker = new();

    private record User(string Username, string PasswordHash);

    private static List<User> Load()
    {
        lock (locker)
        {
            if (!File.Exists(FilePath))
            {
                var list = new List<User> { new User("admin", Hash("admin")) };
                Save(list);
                return list;
            }

            var json = File.ReadAllText(FilePath);
            try
            {
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            catch
            {
                return new List<User>();
            }
        }
    }

    private static void Save(List<User> list)
    {
        lock (locker)
        {
            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

    public static bool ValidateUser(string username, string password)
    {
        var hash = Hash(password);
        var users = Load();
        return users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.PasswordHash == hash);
    }

    public static bool AddUser(string username, string password, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(username)) { error = "Username required"; return false; }
        if (string.IsNullOrEmpty(password)) { error = "Password required"; return false; }
        lock (locker)
        {
            var users = Load();
            if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))) { error = "User already exists"; return false; }
            users.Add(new User(username, Hash(password)));
            Save(users);
            return true;
        }
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
