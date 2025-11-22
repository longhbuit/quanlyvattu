using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace QuanLyVatTu
{
    // Simple runner that reads .sql files from the project's `script` folder and executes them
    // against the three configured branch databases (CongTy, ChiNhanh1, ChiNhanh2).
    public class MultiDbScriptRunner
    {
        // Split batches on lines that consist only of GO (case-insensitive)
        private static string[] SplitBatches(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return Array.Empty<string>();
            // Match lines that contain only GO (possibly surrounded by whitespace). Use Regex.Split.
            var splitter = new Regex(@"^\s*GO\s*(?:\r?\n|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return splitter.Split(sql);
        }

        private static IEnumerable<string> GetSqlFiles(string folder)
        {
            if (!Directory.Exists(folder)) yield break;
            foreach (var f in Directory.EnumerateFiles(folder, "*.sql").OrderBy(p => p))
            {
                yield return f;
            }
        }

        // New: utility to print the three database environment variables. This avoids adding a second Main entry point.
        public static void PrintConnectionStrings()
        {
            LoadDotEnv();
            string? cty = Environment.GetEnvironmentVariable("DATABASE_CTY_CONNECTION_STRING");
            string? cn1 = Environment.GetEnvironmentVariable("DATABASE_CN1_CONNECTION_STRING");
            string? cn2 = Environment.GetEnvironmentVariable("DATABASE_CN2_CONNECTION_STRING");

            Console.WriteLine("DATABASE_CTY_CONNECTION_STRING: " + (cty ?? "<null>"));
            Console.WriteLine("DATABASE_CN1_CONNECTION_STRING: " + (cn1 ?? "<null>"));
            Console.WriteLine("DATABASE_CN2_CONNECTION_STRING: " + (cn2 ?? "<null>"));

            // If you want to run the CTY script here, locate the project directory (where the .csproj lives)


            RunScript("0__CTY.sql", cty);
            RunScript("0__CN1.sql", cn1);
            RunScript("0__CN2.sql", cn2);
            RunScript("1__CTY.sql", cty);
            RunScript("1__CN1.sql", cn1);
            RunScript("1__CN2.sql", cn2);
            RunScript("2__CTY.sql", cty);
            RunScript("2__CN1.sql", cn1);
            RunScript("2__CN2.sql", cn2);
            RunScript("3__CTY.sql", cty);
            RunScript("3__CN1.sql", cn1);
            RunScript("3__CN2.sql", cn2);
            RunScript("9__CTY.sql", cty);
            RunScript("9__CN1.sql", cn1);
            RunScript("9__CN2.sql", cn2);
        }

        private static void RunScript(string script, string cty)
        {
            try
            {
                var projectDir = FindProjectDir() ?? AppContext.BaseDirectory;
                var scriptPath = Path.Combine(projectDir, "script", script);
                if (string.IsNullOrWhiteSpace(cty))
                {
                    Console.Error.WriteLine("CTY connection string is not set; cannot run script.");
                    return;
                }

                var sql = File.ReadAllText(scriptPath);
                var batches = SplitBatches(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));

                using var connection = new SqlConnection(cty);
                connection.Open();
                foreach (var batch in batches)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = batch;
                    cmd.CommandType = CommandType.Text;
                    try
                    {
                        Console.WriteLine("Executing batch...");
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Batch execution failed: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log full exception to help debugging (message + stack trace)
                Console.Error.WriteLine("Failed to execute CTY script: " + ex.ToString());
            }
        }

        // Find the project directory by searching upwards for the project file (QuanLyVatTu.csproj).
        private static string? FindProjectDir()
        {
            try
            {
                var starts = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
                foreach (var start in starts)
                {
                    var dir = new DirectoryInfo(start);
                    while (dir != null)
                    {
                        var candidate = Path.Combine(dir.FullName, "QuanLyVatTu.csproj");
                        if (File.Exists(candidate)) return dir.FullName;
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

        private static string? FindDotEnvPath()
        {
            try
            {
                var dir = new DirectoryInfo("C:\\Users\\longh\\RiderProjects\\QuanLyVatTu\\QuanLyVatTu");
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, ".env");
                    if (File.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static void LoadDotEnv()
        {
            try
            {
                var path = FindDotEnvPath();
                Console.WriteLine(path);
                if (path is null) return;
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                    // support lines like: export FOO=bar
                    if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                        line = line.Substring(7).TrimStart();
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
    }
}