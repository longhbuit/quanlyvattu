USE CTY;
GO

-------------------------------------------------------------
-- Trigger Nhân Viên
-------------------------------------------------------------
IF OBJECT_ID('TR_NhanVien_Replicate', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_NhanVien_Replicate;
GO

CREATE TRIGGER TR_NhanVien_Replicate
    ON dbo.NhanVien
    AFTER INSERT, UPDATE
    AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MANV CHAR(10), @Ho NVARCHAR(40), @Ten NVARCHAR(10), @MaCN CHAR(3);
    DECLARE @IsDeleted BIT; -- Thêm biến hứng trạng thái xóa

    SELECT TOP 1 @MANV = MANV,
                 @Ho = Ho,
                 @Ten = Ten,
                 @MaCN = MaCN,
                 @IsDeleted = IsDeleted -- Lấy giá trị từ inserted
    FROM inserted;

    IF @MANV IS NULL RETURN;

    -- CN1
    IF @MaCN = 'CN1'
        BEGIN
            -- Truyền thêm @IsDeleted sang server đích
            EXEC LINK_CN1.CN1.dbo.SP_Receiver_NhanVien @MANV, @Ho, @Ten, @IsDeleted;
        END

    -- CN2
    IF @MaCN = 'CN2'
        BEGIN
            -- Truyền thêm @IsDeleted sang server đích
            EXEC LINK_CN2.CN2.dbo.SP_Receiver_NhanVien @MANV, @Ho, @Ten, @IsDeleted;
        END
END
GO

-------------------------------------------------------------
-- Trigger Kho
-------------------------------------------------------------

IF OBJECT_ID('dbo.TR_Kho_Replicate', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Kho_Replicate;
GO

CREATE TRIGGER TR_Kho_Replicate
    ON dbo.Kho
    AFTER INSERT, UPDATE
    AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MAKHO CHAR(4), @TenKho NVARCHAR(50), @MaCN CHAR(3);
    DECLARE @IsDeleted BIT;

    SELECT TOP 1 @MAKHO = MAKHO,
                 @TenKho = TenKho,
                 @MaCN = MaCN,
                 @IsDeleted = IsDeleted
    FROM inserted;

    IF @MAKHO IS NULL RETURN;

    IF @MaCN = 'CN1'
        BEGIN
            EXEC LINK_CN1.CN1.dbo.SP_Receiver_Kho @MAKHO, @TenKho, @IsDeleted;
        END

    IF @MaCN = 'CN2'
        BEGIN
            EXEC LINK_CN2.CN2.dbo.SP_Receiver_Kho @MAKHO, @TenKho, @IsDeleted;
        END
END
GO

-------------------------------------------------------------
-- Trigger Vật Tư
-------------------------------------------------------------
IF OBJECT_ID('dbo.TR_Vattu_Replicate', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Vattu_Replicate;
GO

CREATE TRIGGER TR_Vattu_Replicate
    ON dbo.Vattu
    AFTER INSERT, UPDATE
    AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MAVT CHAR(4), @TenVT NVARCHAR(50), @DVT NVARCHAR(20);
    DECLARE @IsDeleted BIT;

    SELECT TOP 1 @MAVT = MAVT,
                 @TenVT = TenVT,
                 @DVT = DVT,
                 @IsDeleted = IsDeleted
    FROM inserted;

    IF @MAVT IS NULL RETURN;

    EXEC LINK_CN1.CN1.dbo.SP_Receiver_Vattu @MAVT, @TenVT, @DVT, @IsDeleted;
    EXEC LINK_CN2.CN2.dbo.SP_Receiver_Vattu @MAVT, @TenVT, @DVT, @IsDeleted;
END
GO

-------------------------------------------------------------
-- Upsert Kho
-------------------------------------------------------------
IF OBJECT_ID('dbo.SP_Kho_Upsert', 'P') IS NOT NULL
    DROP PROC dbo.SP_Kho_Upsert;
GO

CREATE PROCEDURE dbo.SP_Kho_Upsert
    @MAKHO CHAR(4),
    @TenKho NVARCHAR(50),
    @DiaChi NVARCHAR(100),
    @MaCN CHAR(3),
    @IsDeleted BIT = 0 -- Mặc định là 0 (Active)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Kho AS T
    USING (SELECT @MAKHO AS MAKHO, @TenKho AS TenKho, @DiaChi AS DiaChi, @MaCN AS MaCN, @IsDeleted AS IsDeleted) AS S
    ON T.MAKHO = S.MAKHO
    WHEN MATCHED THEN
        UPDATE
        SET TenKho = S.TenKho,
            DiaChi = S.DiaChi,
            MaCN   = S.MaCN,
            IsDeleted = S.IsDeleted -- Cập nhật trạng thái xóa
    WHEN NOT MATCHED THEN
        INSERT (MAKHO, TenKho, DiaChi, MaCN, IsDeleted)
        VALUES (S.MAKHO, S.TenKho, S.DiaChi, S.MaCN, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_Kho_Upsert TO ChiNhanh_Role;
GO

-------------------------------------------------------------
-- Upsert Nhân Viên
-------------------------------------------------------------
IF OBJECT_ID('dbo.SP_NhanVien_Upsert', 'P') IS NOT NULL
    DROP PROC dbo.SP_NhanVien_Upsert;
GO

CREATE PROCEDURE dbo.SP_NhanVien_Upsert
    @MANV    CHAR(10),
    @Ho      NVARCHAR(40),
    @Ten     NVARCHAR(10),
    @DiaChi  NVARCHAR(100),
    @NgaySinh DATE,
    @Luong   FLOAT,
    @MaCN    CHAR(3),
    @IsDeleted BIT = 0 -- Mặc định là 0
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.NhanVien AS T
    USING (
        SELECT @MANV AS MANV, @Ho AS Ho, @Ten AS Ten,
               @DiaChi AS DiaChi, @NgaySinh AS NgaySinh,
               @Luong AS Luong, @MaCN AS MaCN,
               @IsDeleted AS IsDeleted
    ) AS S
    ON T.MANV = S.MANV
    WHEN MATCHED THEN
        UPDATE SET
                   T.IsDeleted = S.IsDeleted,
                   -- Logic Soft Delete:
                   -- Nếu IsDeleted = 1 (Xoá) -> Giữ nguyên thông tin cũ (T)
                   -- Nếu IsDeleted = 0 (Active) -> Update thông tin mới (S)
                   T.Ho       = CASE WHEN S.IsDeleted = 0 THEN S.Ho       ELSE T.Ho       END,
                   T.Ten      = CASE WHEN S.IsDeleted = 0 THEN S.Ten      ELSE T.Ten      END,
                   T.DiaChi   = CASE WHEN S.IsDeleted = 0 THEN S.DiaChi   ELSE T.DiaChi   END,
                   T.NgaySinh = CASE WHEN S.IsDeleted = 0 THEN S.NgaySinh ELSE T.NgaySinh END,
                   T.Luong    = CASE WHEN S.IsDeleted = 0 THEN S.Luong    ELSE T.Luong    END,
                   T.MaCN     = CASE WHEN S.IsDeleted = 0 THEN S.MaCN     ELSE T.MaCN     END
    WHEN NOT MATCHED THEN
        INSERT (MANV, Ho, Ten, DiaChi, NgaySinh, Luong, MaCN, IsDeleted)
        VALUES (S.MANV, S.Ho, S.Ten, S.DiaChi, S.NgaySinh, S.Luong, S.MaCN, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_NhanVien_Upsert TO ChiNhanh_Role;
GO

-------------------------------------------------------------
-- Upsert Vật tư
-------------------------------------------------------------
IF OBJECT_ID('dbo.SP_Vattu_Upsert', 'P') IS NOT NULL
    DROP PROC dbo.SP_Vattu_Upsert;
GO

CREATE PROCEDURE dbo.SP_Vattu_Upsert
    @MAVT   CHAR(4),
    @TenVT  NVARCHAR(50),
    @DVT    NVARCHAR(20),
    @SoLuongTon INT = NULL,   -- NULL nếu không dùng
    @IsDeleted BIT = 0        -- Mặc định là 0
AS

BEGIN
    SET NOCOUNT ON;
    MERGE dbo.Vattu AS T
    USING (
        SELECT @MAVT AS MAVT,
               @TenVT AS TenVT,
               @DVT AS DVT,
               @SoLuongTon AS SoLuongTon,
               @IsDeleted AS IsDeleted
    ) AS S

    ON T.MAVT = S.MAVT
    WHEN MATCHED THEN
        UPDATE SET
                   T.IsDeleted = S.IsDeleted,
                   T.TenVT      = CASE WHEN S.IsDeleted = 0 THEN S.TenVT      ELSE T.TenVT      END,
                   T.DVT        = CASE WHEN S.IsDeleted = 0 THEN S.DVT        ELSE T.DVT        END,
                   T.SoLuongTon = CASE WHEN S.IsDeleted = 0 THEN S.SoLuongTon ELSE T.SoLuongTon END
    WHEN NOT MATCHED THEN
        INSERT (MAVT, TenVT, DVT, SoLuongTon, IsDeleted)
        VALUES (S.MAVT, S.TenVT, S.DVT, S.SoLuongTon, S.IsDeleted);
END
GO

GRANT EXECUTE ON dbo.SP_Vattu_Upsert TO ChiNhanh_Role;
GO
