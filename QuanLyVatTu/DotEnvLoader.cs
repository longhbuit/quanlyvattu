namespace QuanLyVatTu;

using System;
using System.IO;

/// <summary>
/// Shared helper to load a .env file (key=value pairs) into environment variables.
/// This centralizes duplicated logic so both the main project and integration tests can reuse it.
/// </summary>
public static class DotEnvLoader
{
    /// <summary>
    /// Load .env file if present. Existing environment variables are not overwritten.
    /// </summary>
    public static void LoadDotEnv()
    {
        try
        {
            var path = FindDotEnvPath();
            if (path is null) return;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                // support lines like: export FOO=bar
                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase)) line = line.Substring(7).TrimStart();
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;
                var key = line.Substring(0, idx).Trim();
                var val = line.Substring(idx + 1).Trim();
                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                {
                    if (val.Length >= 2) val = val.Substring(1, val.Length - 2);
                }
                // don't override existing environment variables
                if (Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }
        catch
        {
            // best-effort loader: ignore errors
        }
    }

    /// <summary>
    /// Search upwards from likely starting locations for a .env file and return its path or null.
    /// </summary>
    public static string? FindDotEnvPath()
    {
        try
        {
            var starts = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (var start in starts)
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, ".env");
                    if (File.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }
}

