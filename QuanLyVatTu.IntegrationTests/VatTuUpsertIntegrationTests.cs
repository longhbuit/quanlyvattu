namespace QuanLyVatTu.IntegrationTests;

using System;
using System.IO;
using Xunit;
using QuanLyVatTu;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

public class VatTuUpsertIntegrationTests
{
    private static bool ShouldRun() => string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"), "1");

    private static string Env(string name) => Environment.GetEnvironmentVariable(name);
    // Reuse shared loader
    private static void LoadDotEnv() => TestUtils.LoadDotEnv();

    [Fact]
    public void Upsert_InsertsThenUpdatesRecord_And_IsNotDeleted()
    {
        // load .env early so RUN_INTEGRATION_TESTS and credentials can come from the file
        LoadDotEnv();

        if (!ShouldRun())
            return; // skip in normal runs

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "HH:mm:ss "; })
                .SetMinimumLevel(LogLevel.Debug);
        });

        var testLogger = loggerFactory.CreateLogger("VatTuUpsertIntegrationTests");
        testLogger.LogInformation("Starting integration test Upsert_InsertsThenUpdatesRecord_And_IsNotDeleted");

        var adminUser = Env("INTEGRATION_SQL_USER");
        var adminPass = Env("INTEGRATION_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminUser)) throw new InvalidOperationException("INTEGRATION_SQL_USER not set");
        if (adminPass is null) adminPass = string.Empty;

        var branchName = Env("INTEGRATION_BRANCH") ?? "CongTy";
        if (!Enum.TryParse<BranchSite>(branchName, true, out var branch)) branch = BranchSite.CongTy;

        var loginService = new LoginService(loggerFactory.CreateLogger<LoginService>());
        var loginResult = loginService.Login(adminUser, adminPass, branch);
        Assert.True(loginResult.Success, "Admin login failed: " + loginResult.Message);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.ConnectionString));

        testLogger.LogInformation("Admin login succeeded for user {User} on branch {Branch}", adminUser, branch);

        // Wire AppSession so VatTuService picks up the test connection/branch
        AppSession.ConnectionString = loginResult.ConnectionString;
        AppSession.Branch = loginResult.Branch ?? BranchSite.CongTy;

        var connStr = loginResult.ConnectionString!;

        // Generate a stable 4-char MAVT
        var mavt = "T" + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpperInvariant();
        var ten1 = "Test Vattu 1";
        var dvt1 = "Cai";
        var ten2 = "Updated Vattu";
        var dvt2 = "Hop";

        // Helper to cleanup the test row
        void Cleanup()
        {
            try
            {
                using var c = new SqlConnection(connStr);
                c.Open();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "DELETE FROM VatTu WHERE MAVT = @MAVT";
                cmd.Parameters.Add(new SqlParameter("@MAVT", System.Data.SqlDbType.Char, 4) { Value = mavt });
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                testLogger.LogWarning(ex, "Cleanup failed for MAVT={Mavt}", mavt);
            }
        }

        // Ensure a clean start
        Cleanup();

        try
        {
            // 1) Insert
            var ok1 = VatTuService.UpsertVatTu(mavt, ten1, dvt1);
            Assert.True(ok1, "UpsertVatTu returned false on insert");

            // Verify inserted
            using (var c = new SqlConnection(connStr))
            {
                c.Open();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT TenVT, DVT, IsDeleted FROM VatTu WHERE MAVT = @MAVT";
                cmd.Parameters.Add(new SqlParameter("@MAVT", System.Data.SqlDbType.Char, 4) { Value = mavt });
                using var r = cmd.ExecuteReader();
                Assert.True(r.Read(), "Inserted row not found in VatTu");
                var tenDb = r.IsDBNull(0) ? string.Empty : r.GetString(0).Trim();
                var dvtDb = r.IsDBNull(1) ? string.Empty : r.GetString(1).Trim();
                var isDeleted = r.IsDBNull(2) ? false : r.GetBoolean(2);
                Assert.Equal(ten1, tenDb);
                Assert.Equal(dvt1, dvtDb);
                Assert.False(isDeleted, "Inserted row should not be marked deleted");
            }

            // 2) Update
            var ok2 = VatTuService.UpsertVatTu(mavt, ten2, dvt2);
            Assert.True(ok2, "UpsertVatTu returned false on update");

            // Verify updated
            using (var c = new SqlConnection(connStr))
            {
                c.Open();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT TenVT, DVT, IsDeleted FROM VatTu WHERE MAVT = @MAVT";
                cmd.Parameters.Add(new SqlParameter("@MAVT", System.Data.SqlDbType.Char, 4) { Value = mavt });
                using var r = cmd.ExecuteReader();
                Assert.True(r.Read(), "Updated row not found in VatTu");
                var tenDb = r.IsDBNull(0) ? string.Empty : r.GetString(0).Trim();
                var dvtDb = r.IsDBNull(1) ? string.Empty : r.GetString(1).Trim();
                var isDeleted = r.IsDBNull(2) ? false : r.GetBoolean(2);
                Assert.Equal(ten2, tenDb);
                Assert.Equal(dvt2, dvtDb);
                Assert.False(isDeleted, "Updated row should not be marked deleted");
            }
        }
        finally
        {
            // Always attempt cleanup
            Cleanup();
        }
    }
}
