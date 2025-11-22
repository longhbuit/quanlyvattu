namespace QuanLyVatTu;

using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public record UserCreationResult(bool Success, string Message);

public class UserCreationService
{
    private readonly string _connectionString;
    private readonly ILogger<UserCreationService> _logger;

    public UserCreationService(string connectionString, ILogger<UserCreationService>? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? NullLogger<UserCreationService>.Instance;
    }

    /// <summary>
    /// Create a SQL login and corresponding user(s) by invoking the existing stored procedures.
    /// Returns a UserCreationResult describing success/failure and a user-facing message.
    /// </summary>
    public UserCreationResult CreateUser(string loginName, string password, string scope, string role)
    {
        _logger.LogDebug("CreateUser called: login={LoginName}, scope={Scope}, role={Role}", loginName, scope, role);
        try
        {
            var csb = new SqlConnectionStringBuilder(_connectionString);
            var initialDb = (csb.InitialCatalog ?? string.Empty).Trim();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Role restrictions
            if (scope == "Chi Nhánh" && role == "CongTy_Role")
            {
                _logger.LogWarning("Attempt to create CongTy_Role for branch scope: {Login}", loginName);
                return new(false, "Chi nhánh không được tạo tài khoản Công Ty.");
            }

            if (scope == "Công Ty" && !string.Equals(initialDb, "CTY", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CreateUser called for Công Ty but connection initial catalog is {InitialDb}", initialDb);
                return new(false, "Kết nối hiện tại không phải database CTY.");
            }
            if (scope == "Chi Nhánh" && string.Equals(initialDb, "CTY", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CreateUser called for Chi Nhánh while connected to CTY database");
                return new(false, "Đang ở CTY, chọn phạm vi Công Ty hoặc đổi kết nối sang CN.");
            }

            // Check if login exists (server-wide)
            using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.server_principals WHERE name = @name", conn))
            {
                checkCmd.Parameters.AddWithValue("@name", loginName);
                var exists = (int)checkCmd.ExecuteScalar()! > 0;
                if (exists)
                {
                    _logger.LogInformation("CreateUser aborted: login already exists: {Login}", loginName);
                    return new(false, "Login đã tồn tại.");
                }
            }

            if (scope == "Công Ty")
            {
                _logger.LogInformation("Creating company-level login and user: {Login}", loginName);

                using (var spCompany = new SqlCommand("dbo.SP_TaoTaiKhoan_CongTy", conn))
                {
                    spCompany.CommandType = System.Data.CommandType.StoredProcedure;
                    spCompany.Parameters.AddWithValue("@UserName", loginName);
                    spCompany.Parameters.AddWithValue("@Password", password);
                    spCompany.Parameters.AddWithValue("@Role", role);
                    spCompany.ExecuteNonQuery();
                }
            }
            else // Chi Nhánh
            {
                _logger.LogInformation("Creating branch-level user: {Login}", loginName);
                using (var spBranch = new SqlCommand("dbo.SP_TaoTaiKhoan_ChiNhanh", conn))
                {
                    spBranch.CommandType = System.Data.CommandType.StoredProcedure;
                    spBranch.Parameters.AddWithValue("@UserName", loginName);
                    spBranch.Parameters.AddWithValue("@Password", password);
                    spBranch.Parameters.AddWithValue("@Role", role);
                    spBranch.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("CreateUser succeeded for login {Login}", loginName);
            return new(true, "Tạo tài khoản thành công.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error while creating user {Login}", loginName);
            return new(false, "SQL lỗi: " + ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating user {Login}", loginName);
            return new(false, "Không thể tạo login: " + ex.Message);
        }
    }
}
