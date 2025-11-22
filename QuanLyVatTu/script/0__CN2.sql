/* =======================================================
   PHẦN 1: THIẾT LẬP MÔI TRƯỜNG & SERVER ROLE (Chạy tại Master)
   Mục đích: Tạo quyền cấp cao để các User quản lý có thể tạo Login
   ======================================================= */
USE master;
GO

-- 1. Tạo Database CTY nếu chưa có
IF DB_ID('CN2') IS NULL
CREATE DATABASE CN2;
GO

-- 2. Tạo Server Role 'srv_CreateLogin'
-- Role này giúp User quản lý (Giám đốc, Trưởng CN) có quyền tạo Login mà không cần quyền sa (sysadmin)
IF NOT EXISTS (SELECT *
               FROM sys.server_principals
               WHERE name = 'srv_CreateLogin')
    BEGIN
        CREATE SERVER ROLE srv_CreateLogin AUTHORIZATION sa;
    END
GO

-- 3. Cấp các quyền cần thiết cho Server Role này
GRANT ALTER ANY LOGIN TO srv_CreateLogin; -- Cho phép chạy sp_addlogin
GRANT ALTER ANY SERVER ROLE TO srv_CreateLogin; -- Cho phép gán quyền server (nếu cần)
GRANT VIEW SERVER STATE TO srv_CreateLogin; -- Cho phép xem danh sách login
GO

/* =======================================================
   PHẦN 2: THIẾT LẬP DATABASE & ROLES (Chạy tại CTY)
   ======================================================= */
USE CN2
GO

-- 1. Tạo bảng TaiKhoan (Để mapping giữa Login và Nhân viên)
IF OBJECT_ID('TaiKhoan', 'U') IS NOT NULL
    DROP TABLE TaiKhoan
GO

CREATE TABLE TaiKhoan
(
    LoginName VARCHAR(50) PRIMARY KEY
)
GO

-- 2. Dọn dẹp các Role cũ (Tránh lỗi trùng lặp)
IF EXISTS (SELECT *
           FROM sys.database_principals
           WHERE name = 'CongTy_Role')
    EXEC sp_droprole 'CongTy_Role'
IF EXISTS (SELECT *
           FROM sys.database_principals
           WHERE name = 'ChiNhanh_Role')
    EXEC sp_droprole 'ChiNhanh_Role'
IF EXISTS (SELECT *
           FROM sys.database_principals
           WHERE name = 'User_Role')
    EXEC sp_droprole 'User_Role'
GO

-- 3. Tạo 3 Database Role chuẩn (Có hậu tố _Role để tránh trùng keyword)
EXEC sp_addrole 'CongTy_Role' -- Quyền: Xem báo cáo, xem toàn công ty
EXEC sp_addrole 'ChiNhanh_Role' -- Quyền: Toàn quyền tại chi nhánh
EXEC sp_addrole 'User_Role' -- Quyền: Chỉ cập nhật dữ liệu, không tạo user
GRANT CONTROL ON DATABASE::CN2 TO ChiNhanh_Role;
GO

GRANT ALTER ANY USER TO ChiNhanh_Role;
GRANT ALTER ANY ROLE TO ChiNhanh_Role;
GRANT ALTER ON ROLE::ChiNhanh_Role TO ChiNhanh_Role;
GO

/* =======================================================
   PHẦN 3: STORED PROCEDURE TẠO TÀI KHOẢN (Logic Chính)
   ======================================================= */
IF OBJECT_ID('dbo.SP_TaoTaiKhoan_Receiver', 'P') IS NOT NULL
    DROP PROC dbo.SP_TaoTaiKhoan_Receiver
GO
CREATE PROCEDURE dbo.SP_TaoTaiKhoan_Receiver @LoginName VARCHAR(50),
                                             @Password VARCHAR(50),
                                             @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @SQL NVARCHAR(MAX);

        -- Tạo Login nếu chưa có (Đồng bộ pass với Database CN2)
        IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = @LoginName)
            EXEC sp_addlogin @LoginName, @Password, 'CN2';

        -- Tạo User và Gán quyền
        EXEC sp_grantdbaccess @LoginName, @LoginName;
        EXEC sp_addrolemember @Role, @LoginName;

        -- Lưu vào bảng mapping (Bảng này phải có ở CN)
        -- INSERT INTO TaiKhoan(LoginName, MANV) VALUES (@LoginName, @MANV) 
        RETURN 0;
    END TRY
    BEGIN CATCH
        RETURN 1;
    END CATCH
END
GO
GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_Receiver TO PUBLIC
GO

IF OBJECT_ID('dbo.SP_TaoTaiKhoan_ChiNhanh', 'P') IS NOT NULL
    DROP PROC dbo.SP_TaoTaiKhoan_ChiNhanh
GO

CREATE PROCEDURE dbo.SP_TaoTaiKhoan_ChiNhanh @Username VARCHAR(50),
                                             @Password VARCHAR(50),
                                             @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Step NVARCHAR(100) = N'INIT';

    ---------------------------------------------------------
    -- 1. KIỂM TRA QUYỀN
    ---------------------------------------------------------
    SET @Step = N'CHECK PERMISSION';
    IF IS_MEMBER('ChiNhanh_Role') = 0 AND IS_SRVROLEMEMBER('sysadmin') = 0
        BEGIN
            RAISERROR (N'[STEP: %s] Bạn không có quyền thực hiện chức năng này!', 16, 1, @Step);
            RETURN;
        END

    IF @Role = 'CongTy_Role'
        BEGIN
            RAISERROR (N'[STEP: %s] Chi nhánh không được tạo tài khoản Công Ty!', 16, 1, @Step);
            RETURN;
        END

    DECLARE @Ret INT;
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Err NVARCHAR(MAX);
    DECLARE @CurrentStep NVARCHAR(50) = N'START';
    DECLARE @LoginName VARCHAR(50) = N'cn2_'+ @Username;

    BEGIN TRANSACTION;
    BEGIN TRY
        ---------------------------------------------------------
        SET @CurrentStep = N'A1 - CREATE LOGIN';
        ---------------------------------------------------------
        SET @CurrentStep = '[CREATE LOGIN]';
        SET @SQL = N'USE MASTER; CREATE LOGIN [' + @LoginName + N'] WITH PASSWORD = ''' + @Password + N''';';
        EXEC (@SQL);

        ---------------------------------------------------------
        SET @CurrentStep = N'A2 - CREATE USER';
        ---------------------------------------------------------
        SET @CurrentStep = '[CREATE USER]';
        SET @SQL = N'USE CN2; CREATE USER [' + @LoginName + N'] FOR LOGIN [' + @LoginName + N'];';
        EXEC (@SQL);

        ---------------------------------------------------------
        SET @CurrentStep = N'A3 - ADD DATABASE ROLE';
        ---------------------------------------------------------
        SET @SQL =N'USE CN2; ALTER ROLE [' + @Role + N'] ADD MEMBER [' + @LoginName + N'];';
        BEGIN TRY
            EXEC(@SQL);
        END TRY
        BEGIN CATCH
            DECLARE @ErrA2 NVARCHAR(MAX) = ERROR_MESSAGE();
            RAISERROR(N'[ERROR at STEP A2] %s', 16, 1, @ErrA2);
            RETURN;
        END CATCH


        ---------------------------------------------------------
        SET @CurrentStep = N'A4 - ADD SERVER ROLE';
        ---------------------------------------------------------
        SET @SQL = N'USE master; ALTER SERVER ROLE [srv_CreateLogin] ADD MEMBER [' + @LoginName + N'];';

        BEGIN TRY
            EXEC(@SQL);
        END TRY
        BEGIN CATCH
            DECLARE @ErrA3 NVARCHAR(MAX) = ERROR_MESSAGE();
            RAISERROR(N'[ERROR at STEP A3] %s', 16, 1, @ErrA3);
            RETURN;
        END CATCH
        COMMIT TRANSACTION;
        PRINT N'Tạo tài khoản hoàn tất thành công.';

    END TRY
    BEGIN CATCH

        ROLLBACK TRANSACTION;

        SET @Err = ERROR_MESSAGE();

        RAISERROR (N'[STEP: %s] Lỗi khi thiết lập User/Data: %s. Đã hoàn tác toàn bộ.',
            16, 1, @Step, @Err);
    END CATCH

END
GO

GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_ChiNhanh TO ChiNhanh_Role;
GO
