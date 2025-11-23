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
    private static void LoadDotEnv() => TestUtils.LoadDotEnv();

    private static string? FindDotEnvPath() => null; // kept for compatibility if other code calls it

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
