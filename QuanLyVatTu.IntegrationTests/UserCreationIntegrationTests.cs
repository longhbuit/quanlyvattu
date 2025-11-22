namespace QuanLyVatTu.IntegrationTests;

using System;
using System.IO;
using Xunit;
using QuanLyVatTu;
using Microsoft.Extensions.Logging;

public class UserCreationIntegrationTests
{
    private static bool ShouldRun() => string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"), "1");

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name);

    // Load .env file if present. We set env vars only when they're not already defined
    private static void LoadDotEnv()
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

    private static string? FindDotEnvPath()
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
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

    [Fact]
    public void CreateUser_FullFlow_LoginThenCreate()
    {
        // load .env early so RUN_INTEGRATION_TESTS and credentials can come from the file
        LoadDotEnv();

        if (!ShouldRun())
            return; // skip in normal runs

        // Create a console logger factory so we can see logs during integration tests
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "HH:mm:ss "; })
                .SetMinimumLevel(LogLevel.Debug);
        });

        var testLogger = loggerFactory.CreateLogger("UserCreationIntegrationTests");
        testLogger.LogInformation("Starting integration test CreateUser_FullFlow_LoginThenCreate");

        var adminUser = Env("INTEGRATION_SQL_USER");
        var adminPass = Env("INTEGRATION_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminUser)) throw new InvalidOperationException("INTEGRATION_SQL_USER not set");
        if (adminPass is null) adminPass = string.Empty;

        // Optionally override branch via INTEGRATION_BRANCH (CongTy/ChiNhanh1/ChiNhanh2)
        var branchName = Env("INTEGRATION_BRANCH") ?? "CongTy";
        if (!Enum.TryParse<BranchSite>(branchName, true, out var branch)) branch = BranchSite.CongTy;

        // 1) Login as admin to validate credentials and obtain connection string
        var loginService = new LoginService(loggerFactory.CreateLogger<LoginService>());
        var loginResult = loginService.Login(adminUser, adminPass, branch);
        Assert.True(loginResult.Success, "Admin login failed: " + loginResult.Message);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.ConnectionString));

        testLogger.LogInformation("Admin login succeeded for user {User} on branch {Branch}", adminUser, branch);

        // 2) Create a new user using UserCreationService
        var conn = loginResult.ConnectionString!;
        var creationService = new UserCreationService(conn, loggerFactory.CreateLogger<UserCreationService>());
        var newUser = "test_user_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var newPass = "Abcd@1234";
        
        var scope = branch == BranchSite.CongTy ? "Công Ty" : "Chi Nhánh";
        var role = branch == BranchSite.CongTy ? "CongTy_Role" : Env("INTEGRATION_ROLE") ??  "User_Role";

        testLogger.LogInformation("Creating user {NewUser} with scope={Scope} role={Role}", newUser, scope, role);

        var result = creationService.CreateUser(newUser, newPass, scope, role);
        testLogger.LogInformation("CreateUser result: Success={Success} Message={Message}", result.Success, result.Message);
        Assert.True(result.Success, "CreateUser failed: " + result.Message);
    }
}
