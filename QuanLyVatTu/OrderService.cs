using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace QuanLyVatTu;

public static class OrderService
{
    private static string? GetConnectionString()
        => AppSession.ConnectionString ?? ConnectionConfig.GetBase(AppSession.Branch);

    public record OrderLine(string MaVT, int SoLuong, decimal DonGia);

    // Include TrangThai and MaCN in the DTO; MaCN may be null to let DB use its DEFAULT
    public record OrderDto(DateTime Ngay, string NhaCC, string MaNV, string MaKho, int TrangThai, string? MaCN, List<OrderLine> Lines);

    public static int CreateOrder(OrderDto order)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước.");

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var tran = conn.BeginTransaction();
        try
        {
            // Insert DatHang and get inserted MADH
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tran;
            // If MaCN is provided, include it explicitly; otherwise omit MACN so DB default is applied
            if (!string.IsNullOrWhiteSpace(order.MaCN))
            {
                cmd.CommandText = @"INSERT INTO DatHang (Ngay, NhaCC, MANV, MAKHO, TrangThai, MACN)
                                OUTPUT INSERTED.MADH
                                VALUES (@Ngay, @NhaCC, @MANV, @MAKHO, @TrangThai, @MACN)";
                cmd.Parameters.Add(new SqlParameter("@MACN", SqlDbType.NVarChar, 10) { Value = order.MaCN });
            }
            else
            {
                cmd.CommandText = @"INSERT INTO DatHang (Ngay, NhaCC, MANV, MAKHO, TrangThai)
                                OUTPUT INSERTED.MADH
                                VALUES (@Ngay, @NhaCC, @MANV, @MAKHO, @TrangThai)";
            }

            cmd.Parameters.Add(new SqlParameter("@Ngay", SqlDbType.Date) { Value = order.Ngay.Date });
            cmd.Parameters.Add(new SqlParameter("@NhaCC", SqlDbType.NVarChar, 100) { Value = (object?)order.NhaCC ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@MANV", SqlDbType.Char, 10) { Value = (object?)order.MaNV ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@MAKHO", SqlDbType.Char, 4) { Value = (object?)order.MaKho ?? string.Empty });
            cmd.Parameters.Add(new SqlParameter("@TrangThai", SqlDbType.Int) { Value = order.TrangThai });

            var insertedId = Convert.ToInt32(cmd.ExecuteScalar());

            // Insert CTDDH lines
            foreach (var line in order.Lines)
            {
                using var cmdLine = conn.CreateCommand();
                cmdLine.Transaction = tran;
                cmdLine.CommandText = "INSERT INTO CTDDH (MADH, MAVT, SoLuong, DonGia) VALUES (@MADH, @MAVT, @SoLuong, @DonGia)";
                cmdLine.Parameters.Add(new SqlParameter("@MADH", SqlDbType.Int) { Value = insertedId });
                cmdLine.Parameters.Add(new SqlParameter("@MAVT", SqlDbType.Char, 4) { Value = line.MaVT });
                cmdLine.Parameters.Add(new SqlParameter("@SoLuong", SqlDbType.Int) { Value = line.SoLuong });
                cmdLine.Parameters.Add(new SqlParameter("@DonGia", SqlDbType.Money) { Value = line.DonGia });
                cmdLine.ExecuteNonQuery();
            }

            tran.Commit();
            return insertedId;
        }
        catch
        {
            try { tran.Rollback(); } catch { }
            throw;
        }
    }

    public static List<OrderDto> LoadAll()
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước.");

        var list = new List<OrderDto>();
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MADH, Ngay, NhaCC, MANV, MAKHO, TrangThai, MACN FROM DatHang";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            // For quick listing we won't load lines here; caller can LoadLines(madh)
            var ngay = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
            var nhacc = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var manv = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim();
            var makho = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim();
            var trangthai = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
            var macn = reader.IsDBNull(6) ? string.Empty : reader.GetString(6).Trim();
            list.Add(new OrderDto(ngay, nhacc, manv, makho, trangthai, macn, new List<OrderLine>()));
        }
        return list;
    }

    public static List<OrderLine> LoadLines(int maDh)
    {
        var connStr = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connStr))
            throw new InvalidOperationException("Không tìm thấy connection string. Vui lòng đăng nhập trước.");

        var lines = new List<OrderLine>();
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAVT, SoLuong, DonGia FROM CTDDH WHERE MADH = @MADH";
        cmd.Parameters.Add(new SqlParameter("@MADH", SqlDbType.Int) { Value = maDh });
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var mavt = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
            var soluong = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var dongia = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
            lines.Add(new OrderLine(mavt, soluong, dongia));
        }
        return lines;
    }
}
