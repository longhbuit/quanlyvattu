using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace QuanLyVatTu;

// Employee includes separate Ho and Ten fields.
public record Employee(string MaNV, string Ho, string Ten, string? DiaChi = null, DateTime? NgaySinh = null, decimal? Luong = null, string? MaCN = null, bool IsDeleted = false);

public static class EmployeeService
{
    private static string? GetConnectionString()
        => AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);

    // Load all employees. Whether we query central (via LINK_CTY) or local table is
    // decided based on the current AppSession.Branch: branch users will query central.
    public static List<Employee> LoadAll()
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước hoặc cấu hình ConnectionStrings.json.");

        // Decide whether to query central via linked-server.
        var isBranchUser = AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2;

        var list = new List<Employee>();
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        if (isBranchUser)
        {
            // For branch users, read central table via linked-server to get authoritative employee list
            cmd.CommandText = "SELECT MANV, Ho, Ten, DiaChi, NgaySinh, Luong, MACN, IsDeleted FROM LINK_CTY.CTY.dbo.NhanVien WHERE IsDeleted = 0";
        }
        else
        {
            // For CTY user, read directly from local database
            cmd.CommandText = "SELECT MANV, Ho, Ten, DiaChi, NgaySinh, Luong, MACN, IsDeleted FROM NhanVien WHERE IsDeleted = 0";
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // MANV
            var manv = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
            // Ho + Ten -> HoTen
            var ho = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetString(1).Trim() : string.Empty;
            var ten = reader.FieldCount > 2 && !reader.IsDBNull(2) ? reader.GetString(2).Trim() : string.Empty;
            // DiaChi
            var diachi = reader.FieldCount > 3 && !reader.IsDBNull(3) ? reader.GetString(3) : null;
            // NgaySinh
            DateTime? ngaysinh = null;
            if (reader.FieldCount > 4 && !reader.IsDBNull(4))
            {
                try { ngaysinh = reader.GetDateTime(4); } catch { ngaysinh = null; }
            }
            // Luong
            decimal? luong = null;
            if (reader.FieldCount > 5 && !reader.IsDBNull(5))
            {
                try { luong = reader.GetDecimal(5); } catch { luong = null; }
            }
            // MACN
            var macn = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetString(6) : null;
            // IsDeleted
            var isDeleted = false;
            if (reader.FieldCount > 7 && !reader.IsDBNull(7))
            {
                try { isDeleted = reader.GetBoolean(7); } catch { isDeleted = false; }
            }

            list.Add(new Employee(manv, ho, ten, diachi, ngaysinh, luong, macn, isDeleted));
        }

        return list;
    }

    // Updated to accept NgaySinh and Luong and explicit Ho/Ten so we can send them to the central stored-proc
    public static bool UpsertEmployee(string manv, string ho, string ten, string diachi, DateTime? ngaySinh, decimal? luong)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        var macn = AppSession.Branch == BranchSite.ChiNhanh1 ? "CN1" : AppSession.Branch == BranchSite.ChiNhanh2 ? "CN2" : "CTY";

        // ho and ten are provided by caller

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Choose where to execute the procedure: via LINK_CTY for branch users, local dbo for CTY
        var execPrefix = (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2)
            ? "EXEC LINK_CTY.CTY.dbo.SP_NhanVien_Upsert"
            : "EXEC dbo.SP_NhanVien_Upsert";
        cmd.CommandText = execPrefix + " @MANV, @Ho, @Ten, @DiaChi, @NgaySinh, @Luong, @MaCN, @IsDeleted";
        cmd.Parameters.Add(new SqlParameter("@MANV", System.Data.SqlDbType.Char, 10) { Value = (object?)manv ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@Ho", System.Data.SqlDbType.NVarChar, 40) { Value = (object?)ho ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@Ten", System.Data.SqlDbType.NVarChar, 10) { Value = (object?)ten ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@DiaChi", System.Data.SqlDbType.NVarChar, 100) { Value = (object?)diachi ?? string.Empty });
        var pNgay = new SqlParameter("@NgaySinh", System.Data.SqlDbType.Date) { Value = (object?)ngaySinh ?? DBNull.Value };
        cmd.Parameters.Add(pNgay);
        // SP expects FLOAT; convert decimal? to double if present
        var pLuong = new SqlParameter("@Luong", System.Data.SqlDbType.Float);
        if (luong.HasValue) pLuong.Value = (double)luong.Value; else pLuong.Value = DBNull.Value;
        cmd.Parameters.Add(pLuong);
        cmd.Parameters.Add(new SqlParameter("@MaCN", System.Data.SqlDbType.Char, 3) { Value = macn });
        // default IsDeleted = 0 for upsert
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = 0 });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }

    public static bool DeleteLocalEmployee(string manv)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        var macn = AppSession.Branch == BranchSite.ChiNhanh1 ? "CN1" : AppSession.Branch == BranchSite.ChiNhanh2 ? "CN2" : "CTY";

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Call SP_NhanVien_Upsert with IsDeleted = 1. Use LINK_CTY for branch users so the call runs on CTY.
        var execPrefix = (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2)
            ? "EXEC LINK_CTY.CTY.dbo.SP_NhanVien_Upsert"
            : "EXEC dbo.SP_NhanVien_Upsert";
        cmd.CommandText = execPrefix + " @MANV, @Ho, @Ten, @DiaChi, @NgaySinh, @Luong, @MaCN, @IsDeleted";
        cmd.Parameters.Add(new SqlParameter("@MANV", System.Data.SqlDbType.Char, 10) { Value = (object?)manv ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@Ho", System.Data.SqlDbType.NVarChar, 40) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@Ten", System.Data.SqlDbType.NVarChar, 10) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@DiaChi", System.Data.SqlDbType.NVarChar, 100) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@NgaySinh", System.Data.SqlDbType.Date) { Value = DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Luong", System.Data.SqlDbType.Float) { Value = DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@MaCN", System.Data.SqlDbType.Char, 3) { Value = macn });
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = 1 });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }
}
