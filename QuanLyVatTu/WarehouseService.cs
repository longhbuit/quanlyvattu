using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;

namespace QuanLyVatTu;

public record Warehouse(string MaKho, string TenKho, string? DiaChi = null);

public static class WarehouseService
{
    private static string? GetConnectionString()
        => AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);

    public static List<Warehouse> LoadAll()
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước.");

        var list = new List<Warehouse>();
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Only load warehouses that are not marked deleted
        cmd.CommandText = "SELECT MAKHO, TenKho FROM Kho WHERE IsDeleted = 0";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var ma = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
            var ten = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            list.Add(new Warehouse(ma, ten));
        }
        return list;
    }

    public static bool UpsertWarehouse(string makho, string tenKho, string diachi)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        var macn = AppSession.Branch == BranchSite.ChiNhanh1 ? "CN1" : AppSession.Branch == BranchSite.ChiNhanh2 ? "CN2" : "CTY";

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Call stored procedure on central server via linked server LINK_CTY
        cmd.CommandText = "EXEC LINK_CTY.CTY.dbo.SP_Kho_Upsert @MAKHO, @TenKho, @DiaChi, @MaCN";
        cmd.Parameters.Add(new SqlParameter("@MAKHO", System.Data.SqlDbType.Char, 4) { Value = makho });
        cmd.Parameters.Add(new SqlParameter("@TenKho", System.Data.SqlDbType.NVarChar, 50) { Value = tenKho });
        cmd.Parameters.Add(new SqlParameter("@DiaChi", System.Data.SqlDbType.NVarChar, 100) { Value = (object?)diachi ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@MaCN", System.Data.SqlDbType.Char, 3) { Value = macn });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }

    public static bool DeleteLocalWarehouse(string makho)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        var macn = AppSession.Branch == BranchSite.ChiNhanh1 ? "CN1" : AppSession.Branch == BranchSite.ChiNhanh2 ? "CN2" : "CTY";

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Delete by marking as deleted on central server via linked stored-proc (IsDeleted=1)
        cmd.CommandText = "EXEC LINK_CTY.CTY.dbo.SP_Kho_Upsert @MAKHO, @TenKho, @DiaChi, @MaCN, @IsDeleted";
        cmd.Parameters.Add(new SqlParameter("@MAKHO", System.Data.SqlDbType.Char, 4) { Value = makho });
        // no local name/details when deleting — pass empty strings
        cmd.Parameters.Add(new SqlParameter("@TenKho", System.Data.SqlDbType.NVarChar, 50) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@DiaChi", System.Data.SqlDbType.NVarChar, 100) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@MaCN", System.Data.SqlDbType.Char, 3) { Value = macn });
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = 1 });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }
}
