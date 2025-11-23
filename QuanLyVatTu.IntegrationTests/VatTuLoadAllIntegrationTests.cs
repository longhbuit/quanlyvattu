namespace QuanLyVatTu.IntegrationTests;

using System;
using System.Linq;
using Xunit;
using QuanLyVatTu;
using Microsoft.Extensions.Logging;

public class VatTuLoadAllIntegrationTests
{
    private static bool ShouldRun() => string.Equals(Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"), "1");
    private static string Env(string name) => Environment.GetEnvironmentVariable(name);
    private static void LoadDotEnv() => TestUtils.LoadDotEnv();

    [Fact]
    public void LoadAll_Returns_Inserted_Record()
    {
        LoadDotEnv();
        if (!ShouldRun()) return; // skip when not running integration tests

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "HH:mm:ss "; })
                .SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger("VatTuLoadAllIntegrationTests");
        logger.LogInformation("Starting LoadAll_Returns_Inserted_Record");

        var adminUser = Env("INTEGRATION_SQL_USER");
        var adminPass = Env("INTEGRATION_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminUser)) throw new InvalidOperationException("INTEGRATION_SQL_USER not set");
        if (adminPass is null) adminPass = string.Empty;

        var branchName = Env("INTEGRATION_BRANCH") ?? "CongTy";
        if (!Enum.TryParse<BranchSite>(branchName, true, out var branch)) branch = BranchSite.CongTy;

        var loginService = new LoginService(loggerFactory.CreateLogger<LoginService>());
        var loginResult = loginService.Login(adminUser, adminPass, branch);
        Assert.True(loginResult.Success, "Admin login failed: " + loginResult.Message);

        // Wire session for VatTuService
        AppSession.ConnectionString = loginResult.ConnectionString;
        AppSession.Branch = loginResult.Branch ?? BranchSite.CongTy;

        var mavt = "L" + Guid.NewGuid().ToString("N").Substring(0, 3).ToUpperInvariant();
        var ten = "LoadAll Test";
        var dvt = "Cai";

        // Ensure clean state (try both service delete and direct DB if necessary)
        try
        {
            // Attempt to ensure no pre-existing record
            try
            {
                VatTuService.DeleteLocalVatTu(mavt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Pre-clean failed for MAVT={Mavt}", mavt);
            }

            // Insert using Upsert
            var inserted = VatTuService.UpsertVatTu(mavt, ten, dvt);
            Assert.True(inserted, "UpsertVatTu did not return true");

            // Load all and assert our MAVT is present
            var all = VatTuService.LoadAll();
            Assert.NotNull(all);
            var found = all.FirstOrDefault(v => string.Equals(v.MaVT.Trim(), mavt.Trim(), StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(found);
            Assert.Equal(mavt.Trim(), found.MaVT.Trim());
            Assert.Equal(ten, found.TenVT);
        }
        finally
        {
            // cleanup
            try
            {
                VatTuService.DeleteLocalVatTu(mavt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cleanup failed for MAVT={Mavt}", mavt);
            }
        }
    }
}
