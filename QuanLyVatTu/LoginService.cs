namespace QuanLyVatTu;

using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public record LoginResult(bool Success, string Message, string? ConnectionString = null, BranchSite? Branch = null, string? Username = null);

public class LoginService
{
    private readonly ILogger<LoginService> _logger;

    public LoginService(ILogger<LoginService>? logger = null)
    {
        _logger = logger ?? NullLogger<LoginService>.Instance;
    }

    /// <summary>
    /// Attempt to build a connection string for the given branch and credentials, and open a connection to validate them.
    /// Returns LoginResult indicating success and the validated connection string on success.
    /// </summary>
    public LoginResult Login(string user, string password, BranchSite branch)
    {
        _logger.LogDebug("Login called for user '{User}' and branch {Branch}", user, branch);

        if (string.IsNullOrWhiteSpace(user))
        {
            _logger.LogWarning("Login attempt with empty username");
            return new(false, "Nhập SQL User.");
        }
        if (password is null) password = string.Empty;

        var baseConn = ConnectionConfig.GetBase(branch);
        if (string.IsNullOrWhiteSpace(baseConn))
        {
            _logger.LogError("No base connection string found for branch {Branch}", branch);
            return new(false, "Không tìm thấy connection string cho chi nhánh.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(baseConn)
            {
                UserID = user,
                Password = password
            };
            _logger.LogDebug("Built connection string for user '{User}' (DataSource={DataSource}, InitialCatalog={InitialCatalog})", user, builder.DataSource, builder.InitialCatalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid base connection string for branch {Branch}", branch);
            return new(false, "Connection string không hợp lệ: " + ex.Message);
        }

        try
        {
            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            _logger.LogInformation("SQL connection successful for user '{User}' on branch {Branch}", user, branch);
            return new(true, "Kết nối thành công.", builder.ConnectionString, branch, user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open SQL connection for user '{User}' on branch {Branch}", user, branch);
            return new(false, "Kết nối SQL thất bại: " + ex.Message);
        }
    }
}
