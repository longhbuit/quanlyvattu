USE CN2;

-------------------------------------------
-- SYNC Kho
-------------------------------------------
IF OBJECT_ID('SP_Receiver_Kho', 'P') IS NOT NULL
    DROP PROC SP_Receiver_Kho
GO
CREATE PROCEDURE dbo.SP_Receiver_Kho
    @MAKHO CHAR(4),
    @TenKho NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

MERGE dbo.Kho AS T
    USING (SELECT @MAKHO AS MAKHO, @TenKho AS TenKho) AS S
    ON T.MAKHO = S.MAKHO
    WHEN MATCHED THEN UPDATE SET TenKho = S.TenKho
                          WHEN NOT MATCHED THEN INSERT (MAKHO, TenKho)
                      VALUES (S.MAKHO, S.TenKho);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_Kho TO PUBLIC
GO

-------------------------------------------
-- SYNC NhanVien
-------------------------------------------
IF OBJECT_ID('SP_Receiver_NhanVien', 'P') IS NOT NULL
    DROP PROC SP_Receiver_NhanVien
GO
CREATE PROCEDURE dbo.SP_Receiver_NhanVien
    @MANV CHAR(10),
    @Ho NVARCHAR(40),
    @Ten NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HoTen NVARCHAR(60) = @Ho + ' ' + @Ten;

MERGE dbo.NhanVien AS T
    USING (SELECT @MANV AS MANV, @HoTen AS HoTen) AS S
    ON T.MANV = S.MANV
    WHEN MATCHED THEN
UPDATE SET HoTen = S.HoTen
    WHEN NOT MATCHED THEN
INSERT (MANV, HoTen)
VALUES (S.MANV, S.HoTen);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_NhanVien TO PUBLIC
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
    @DVT NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

MERGE dbo.VatTu AS T
    USING (
    SELECT @MAVT AS MAVT,
    @TenVT AS TenVT,
    @DVT AS DVT
    ) AS S
    ON T.MAVT = S.MAVT
    WHEN MATCHED THEN
UPDATE SET TenVT = S.TenVT,
    DVT = S.DVT
    WHEN NOT MATCHED THEN
INSERT (MAVT, TenVT, DVT)
VALUES (S.MAVT, S.TenVT, S.DVT);
END
GO

GRANT EXECUTE ON dbo.SP_Receiver_Vattu TO PUBLIC
GO