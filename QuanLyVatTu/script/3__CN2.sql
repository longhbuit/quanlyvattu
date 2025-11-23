USE CN2;

-------------------------------------------
-- SYNC Kho
-------------------------------------------
IF OBJECT_ID('dbo.SP_Receiver_Kho', 'P') IS NOT NULL DROP PROC dbo.SP_Receiver_Kho;
GO

CREATE PROCEDURE dbo.SP_Receiver_Kho
    @MAKHO CHAR(4),
    @TenKho NVARCHAR(50),
    @IsDeleted BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Kho AS T
    USING (SELECT @MAKHO AS MAKHO, @TenKho AS TenKho, @IsDeleted AS IsDeleted) AS S
    ON T.MAKHO = S.MAKHO
    WHEN MATCHED THEN
        UPDATE SET
                   T.IsDeleted = S.IsDeleted,
                   -- Logic: Nếu IsDeleted = 0 (Active) thì lấy tên mới (S), 
                   --        Nếu IsDeleted = 1 (Delete) thì giữ nguyên tên cũ (T)
                   T.TenKho = CASE WHEN S.IsDeleted = 0 THEN S.TenKho ELSE T.TenKho END
    WHEN NOT MATCHED THEN
        -- Trường hợp chưa có thì Insert luôn trạng thái IsDeleted truyền vào
        INSERT (MAKHO, TenKho, IsDeleted)
        VALUES (S.MAKHO, S.TenKho, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_Kho TO PUBLIC
GO

-------------------------------------------
-- SYNC NhanVien
-------------------------------------------
IF OBJECT_ID('dbo.SP_Receiver_NhanVien', 'P') IS NOT NULL
    DROP PROC dbo.SP_Receiver_NhanVien;
GO

CREATE PROCEDURE dbo.SP_Receiver_NhanVien
    @MANV CHAR(10),
    @Ho NVARCHAR(40),
    @Ten NVARCHAR(10),
    @IsDeleted BIT = 0 -- Thêm tham số mặc định 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Gộp Họ và Tên trước khi xử lý
    DECLARE @HoTen NVARCHAR(60) = @Ho + ' ' + @Ten;

    MERGE dbo.NhanVien AS T
    USING (SELECT @MANV AS MANV, @HoTen AS HoTen, @IsDeleted AS IsDeleted) AS S
    ON T.MANV = S.MANV
    WHEN MATCHED THEN
        UPDATE SET
                   T.IsDeleted = S.IsDeleted,
                   -- Logic Soft Delete:
                   -- Nếu IsDeleted = 0 (Active) -> Cập nhật HoTen mới từ S
                   -- Nếu IsDeleted = 1 (Delete) -> Giữ nguyên HoTen cũ của T
                   T.HoTen = CASE WHEN S.IsDeleted = 0 THEN S.HoTen ELSE T.HoTen END
    WHEN NOT MATCHED THEN
        INSERT (MANV, HoTen, IsDeleted)
        VALUES (S.MANV, S.HoTen, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_NhanVien TO PUBLIC
GO

GRANT EXECUTE ON dbo.SP_Receiver_NhanVien TO [ChiNhanh_Role]
GO

-------------------------------------------
-- SYNC Vat Tu
-------------------------------------------
IF OBJECT_ID('SP_Receiver_Vattu', 'P') IS NOT NULL
    DROP PROC SP_Receiver_Vattu
GO
CREATE PROCEDURE dbo.SP_Receiver_Vattu
    @MAVT CHAR(4),
    @TenVT NVARCHAR(50),
    @DVT NVARCHAR(20),
    @IsDeleted BIT = 0 -- Thêm tham số IsDeleted
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.VatTu AS T
    USING (
        SELECT @MAVT AS MAVT,
               @TenVT AS TenVT,
               @DVT AS DVT,
               @IsDeleted AS IsDeleted
    ) AS S
    ON T.MAVT = S.MAVT
    WHEN MATCHED THEN
        UPDATE SET
                   T.IsDeleted = S.IsDeleted,
                   -- Logic: Nếu IsDeleted = 1 (đang xoá) thì giữ nguyên dữ liệu cũ (T)
                   --        Nếu IsDeleted = 0 (đang active) thì cập nhật dữ liệu mới (S)
                   T.TenVT = CASE WHEN S.IsDeleted = 0 THEN S.TenVT ELSE T.TenVT END,
                   T.DVT   = CASE WHEN S.IsDeleted = 0 THEN S.DVT   ELSE T.DVT   END
    WHEN NOT MATCHED THEN
        INSERT (MAVT, TenVT, DVT, IsDeleted)
        VALUES (S.MAVT, S.TenVT, S.DVT, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_Vattu TO PUBLIC
GO

GRANT EXECUTE ON dbo.SP_Receiver_Vattu TO [ChiNhanh_Role]
GO
