using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace QuanLyVatTu;

public record VatTu(string MaVT, string TenVT, string? DVT = null, double? SoLuongTon = null, double? DonGia = null, bool IsDeleted = false);

public static class VatTuService
{
    private static string? GetConnectionString()
        => AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);

    public static List<VatTu> LoadAll()
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước.");

        var list = new List<VatTu>();
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // For branch users query central via linked server, otherwise local
        var isBranchUser = AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2;
        if (isBranchUser)
        {
            // Exclude records that do not have a MaCN (MACN is null/empty or whitespace)
            cmd.CommandText = "SELECT MAVT, TenVT, DVT, SOLUONGTON, NULL AS DonGia, IsDeleted FROM LINK_CTY.CTY.dbo.VatTu WHERE IsDeleted = 0";
        }
        else
        {
            cmd.CommandText = "SELECT MAVT, TenVT, DVT, SOLUONGTON, NULL AS DonGia, IsDeleted FROM VatTu WHERE IsDeleted = 0";
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var mavt = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
            var ten = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetString(1).Trim() : string.Empty;
            var dvt = reader.FieldCount > 2 && !reader.IsDBNull(2) ? reader.GetString(2) : null;
            double? sl = null;
            if (reader.FieldCount > 3 && !reader.IsDBNull(3))
            {
                try { sl = Convert.ToDouble(reader.GetValue(3)); } catch { sl = null; }
            }
            double? dongia = null;
            if (reader.FieldCount > 4 && !reader.IsDBNull(4))
            {
                try { dongia = Convert.ToDouble(reader.GetValue(4)); } catch { dongia = null; }
            }
            var isDeleted = false;
            if (reader.FieldCount > 6 && !reader.IsDBNull(5))
            {
                try { isDeleted = reader.GetBoolean(5); } catch { isDeleted = false; }
            }

            list.Add(new VatTu(mavt, ten, dvt, sl, dongia, isDeleted));
        }

        return list;
    }

    public static bool UpsertVatTu(string mavt, string tenvt, string dvt)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        // No local macn needed here; stored procedure will handle branch logic via LINK_CTY when required.

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        // Call central procedure via LINK_CTY for branch users, local otherwise
        // Use SP_Vattu_Upsert as requested. Procedure signature:
        // dbo.SP_Vattu_Upsert @MAVT CHAR(4), @TenVT NVARCHAR(50), @DVT NVARCHAR(20), @SoLuongTon INT = NULL, @IsDeleted BIT = 0
        var execPrefix = (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2)
            ? "EXEC LINK_CTY.CTY.dbo.SP_Vattu_Upsert"
            : "EXEC dbo.SP_Vattu_Upsert";
        cmd.CommandText = execPrefix + " @MAVT, @TenVT, @DVT, @SoLuongTon, @IsDeleted";
        cmd.Parameters.Add(new SqlParameter("@MAVT", System.Data.SqlDbType.Char, 4) { Value = (object?)mavt ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@TenVT", System.Data.SqlDbType.NVarChar, 50) { Value = (object?)tenvt ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@DVT", System.Data.SqlDbType.NVarChar, 20) { Value = (object?)dvt ?? string.Empty });
        // Procedure supports @SoLuongTon INT = NULL. We don't have the value here, so send DBNull.Value to pass NULL.
        cmd.Parameters.Add(new SqlParameter("@SoLuongTon", System.Data.SqlDbType.Int) { Value = DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = 0 });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }

    public static bool DeleteLocalVatTu(string mavt)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string.");

        // No local macn needed here; stored procedure will handle branch logic via LINK_CTY when required.

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        var execPrefix = (AppSession.Branch == BranchSite.ChiNhanh1 || AppSession.Branch == BranchSite.ChiNhanh2)
            ? "EXEC LINK_CTY.CTY.dbo.SP_Receiver_Vattu"
            : "EXEC dbo.SP_Receiver_Vattu";
        cmd.CommandText = execPrefix + " @MAVT, @TenVT, @DVT, @IsDeleted";
        cmd.Parameters.Add(new SqlParameter("@MAVT", System.Data.SqlDbType.Char, 4) { Value = (object?)mavt ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@TenVT", System.Data.SqlDbType.NVarChar, 50) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@DVT", System.Data.SqlDbType.NVarChar, 20) { Value = string.Empty });
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = 1 });
        cmd.CommandTimeout = 30;
        cmd.ExecuteNonQuery();
        return true;
    }
}
