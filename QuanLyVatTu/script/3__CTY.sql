USE CTY;

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

    SELECT TOP 1 @MANV = MANV,
                 @Ho = Ho,
                 @Ten = Ten,
                 @MaCN = MaCN
    FROM inserted;

    IF @MANV IS NULL RETURN;

    -- CN1
    IF @MaCN = 'CN1'
        BEGIN
            EXEC LINK_CN1.QLVT_CN1.dbo.SP_Receiver_NhanVien @MANV, @Ho, @Ten;
        END

    IF @MaCN = 'CN2'
        BEGIN
            EXEC LINK_CN2.QLVT_CN2.dbo.SP_Receiver_NhanVien @MANV, @Ho, @Ten;
        END
    -- CN2
END
GO

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

    SELECT TOP 1 @MAKHO = MAKHO,
                 @TenKho = TenKho,
                 @MaCN = MaCN
    FROM inserted;

    IF @MAKHO IS NULL RETURN;

    IF @MaCN = 'CN1'
        BEGIN
            EXEC LINK_CN1.QLVT_CN1.dbo.SP_Receiver_Kho @MAKHO, @TenKho;
        END

    IF @MaCN = 'CN2'
        BEGIN
            EXEC LINK_CN2.QLVT_CN2.dbo.SP_Receiver_Kho @MAKHO, @TenKho;
        END
END
GO


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

    SELECT TOP 1 @MAVT = MAVT,
                 @TenVT = TenVT,
                 @DVT = DVT
    FROM inserted;

    IF @MAVT IS NULL RETURN;

    EXEC LINK_CN1.QLVT_CN1.dbo.SP_Receiver_Vattu @MAVT, @TenVT, @DVT;
    EXEC LINK_CN2.QLVT_CN2.dbo.SP_Receiver_Vattu @MAVT, @TenVT, @DVT;
END
GO

IF OBJECT_ID('dbo.SP_Kho_Upsert', 'P') IS NOT NULL
    DROP PROC dbo.SP_Kho_Upsert;
GO

CREATE PROCEDURE dbo.SP_Kho_Upsert @MAKHO CHAR(4),
                                   @TenKho NVARCHAR(50),
                                   @DiaChi NVARCHAR(100),
                                   @MaCN CHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Kho AS T
    USING (SELECT @MAKHO AS MAKHO, @TenKho AS TenKho, @DiaChi AS DiaChi, @MaCN AS MaCN) AS S
    ON T.MAKHO = S.MAKHO
    WHEN MATCHED THEN
        UPDATE
        SET TenKho = S.TenKho,
            DiaChi = S.DiaChi,
            MaCN   = S.MaCN
    WHEN NOT MATCHED THEN
        INSERT (MAKHO, TenKho, DiaChi, MaCN)
        VALUES (S.MAKHO, S.TenKho, S.DiaChi, S.MaCN);
END
GO

GRANT EXECUTE ON dbo.SP_Kho_Upsert TO PUBLIC
GO


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
    @MaCN    CHAR(3)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.NhanVien AS T
    USING (
        SELECT @MANV AS MANV, @Ho AS Ho, @Ten AS Ten,
               @DiaChi AS DiaChi, @NgaySinh AS NgaySinh,
               @Luong AS Luong, @MaCN AS MaCN
    ) AS S
    ON T.MANV = S.MANV
    WHEN MATCHED THEN
        UPDATE SET Ho = S.Ho,
                   Ten = S.Ten,
                   DiaChi = S.DiaChi,
                   NgaySinh = S.NgaySinh,
                   Luong = S.Luong,
                   MaCN = S.MaCN
    WHEN NOT MATCHED THEN
        INSERT (MANV, Ho, Ten, DiaChi, NgaySinh, Luong, MaCN)
        VALUES (S.MANV, S.Ho, S.Ten, S.DiaChi, S.NgaySinh, S.Luong, S.MaCN);
END
GO

GRANT EXECUTE ON dbo.SP_NhanVien_Upsert TO PUBLIC;
GO


IF OBJECT_ID('dbo.SP_Vattu_Upsert', 'P') IS NOT NULL
    DROP PROC dbo.SP_Vattu_Upsert;
GO

CREATE PROCEDURE dbo.SP_Vattu_Upsert
    @MAVT   CHAR(4),
    @TenVT  NVARCHAR(50),
    @DVT    NVARCHAR(20),
    @SoLuongTon INT = NULL   -- NULL nếu không dùng
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.Vattu AS T
    USING (
        SELECT @MAVT AS MAVT, @TenVT AS TenVT, @DVT AS DVT, @SoLuongTon AS SoLuongTon
    ) AS S
    ON T.MAVT = S.MAVT
    WHEN MATCHED THEN
        UPDATE SET TenVT = S.TenVT,
                   DVT = S.DVT,
                   SoLuongTon = S.SoLuongTon
    WHEN NOT MATCHED THEN
        INSERT (MAVT, TenVT, DVT, SoLuongTon)
        VALUES (S.MAVT, S.TenVT, S.DVT, S.SoLuongTon);
END
GO

GRANT EXECUTE ON dbo.SP_Vattu_Upsert TO PUBLIC;
GO
