/* =======================================================
   PHẦN 1: THIẾT LẬP MÔI TRƯỜNG & SERVER ROLE (Chạy tại Master)
   Mục đích: Tạo quyền cấp cao để các User quản lý có thể tạo Login
   ======================================================= */
USE master;
GO

-- 1. Tạo Database CTY nếu chưa có
IF DB_ID('CTY') IS NULL
CREATE DATABASE CTY;
GO

-- 2. Tạo Server Role 'srv_CreateLogin'
-- Role này giúp User quản lý (Giám đốc, Trưởng CN) có quyền tạo Login mà không cần quyền sa (sysadmin)
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'srv_CreateLogin')
    BEGIN
        CREATE SERVER ROLE srv_CreateLogin AUTHORIZATION sa;
    END
GO

-- 3. Cấp các quyền cần thiết cho Server Role này
GRANT ALTER ANY LOGIN TO srv_CreateLogin;       -- Cho phép chạy sp_addlogin
GRANT ALTER ANY SERVER ROLE TO srv_CreateLogin; -- Cho phép gán quyền server (nếu cần)
GRANT VIEW SERVER STATE TO srv_CreateLogin;     -- Cho phép xem danh sách login
GO

/* =======================================================
   PHẦN 2: THIẾT LẬP DATABASE & ROLES (Chạy tại CTY)
   ======================================================= */
USE CTY
GO

-- 1. Tạo bảng TaiKhoan (Để mapping giữa Login và Nhân viên)
IF OBJECT_ID('TaiKhoan', 'U') IS NOT NULL
    DROP TABLE TaiKhoan
GO

CREATE TABLE TaiKhoan (
                          LoginName VARCHAR(50) PRIMARY KEY,
)
GO

-- 2. Dọn dẹp các Role cũ (Tránh lỗi trùng lặp)
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'CongTy_Role') EXEC sp_droprole 'CongTy_Role'
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'ChiNhanh_Role') EXEC sp_droprole 'ChiNhanh_Role'
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'User_Role') EXEC sp_droprole 'User_Role'
GO

-- 3. Tạo 3 Database Role chuẩn (Có hậu tố _Role để tránh trùng keyword)
EXEC sp_addrole 'CongTy_Role'   -- Quyền: Xem báo cáo, xem toàn công ty
EXEC sp_addrole 'ChiNhanh_Role' -- Quyền: Toàn quyền tại chi nhánh
EXEC sp_addrole 'User_Role'     -- Quyền: Chỉ cập nhật dữ liệu, không tạo user
GO

GRANT ALTER ANY USER TO CongTy_Role;
GRANT ALTER ANY ROLE TO CongTy_Role;
GRANT ALTER ON ROLE::CongTy_Role TO CongTy_Role;

GRANT ALTER ANY USER TO ChiNhanh_Role;
GRANT ALTER ANY ROLE TO ChiNhanh_Role;
GRANT ALTER ON ROLE::ChiNhanh_Role TO ChiNhanh_Role;
GO

/* =======================================================
   PHẦN 3: STORED PROCEDURE TẠO TÀI KHOẢN (Logic Chính)
   ======================================================= */
IF OBJECT_ID('SP_TaoLogin_Global', 'P') IS NOT NULL
    DROP PROC SP_TaoLogin_Global
GO

CREATE PROCEDURE SP_TaoLogin_Global
    @LoginName SYSNAME,
    @Role SYSNAME
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra trùng login
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
        BEGIN
            DECLARE @sql NVARCHAR(MAX);

            -- Tạo login
            SET @sql = N'CREATE LOGIN [' + @LoginName + N'] WITH PASSWORD=''Abcd@1234'';';
            EXEC (@sql);
        END

    BEGIN TRY
        DECLARE @sql2 NVARCHAR(MAX);

        -- Tạo user trong database hiện tại
        SET @sql2 = N'CREATE USER [' + @LoginName + N'] FOR LOGIN [' + @LoginName + N'];';
        EXEC (@sql2);

        -- Add role
        SET @sql2 = N'ALTER ROLE [' + @Role + N'] ADD MEMBER [' + @LoginName + N'];';
        EXEC (@sql2);

        RETURN 0;
    END TRY
    BEGIN CATCH
        RETURN 1;
    END CATCH
END

GRANT EXECUTE ON dbo.SP_TaoLogin_Global TO ChiNhanh_Role;

/* =======================================================
   PHẦN 3: STORED PROCEDURE TẠO TÀI KHOẢN (Logic Chính)
   ======================================================= */
IF OBJECT_ID('SP_TaoTaiKhoan_CongTy', 'P') IS NOT NULL
    DROP PROC SP_TaoTaiKhoan_CongTy
GO

CREATE PROCEDURE SP_TaoTaiKhoan_CongTy
    @UserName VARCHAR(46),
    @Password VARCHAR(50),
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    -------------------------------------------------------------
    -- Biến ghi tổ hợp STEP hiện tại
    -------------------------------------------------------------
    DECLARE @CurrentStep NVARCHAR(50) = N'START';
    DECLARE @SQL NVARCHAR(MAX);

    -------------------------------------------------------------
    -- 1. KIỂM TRA QUYỀN
    -------------------------------------------------------------
    IF IS_MEMBER('CongTy_Role') = 0 AND IS_SRVROLEMEMBER('sysadmin') = 0
        BEGIN
            RAISERROR(N'[STEP START] Chỉ nhóm Công Ty mới được dùng chức năng này!', 16, 1);
            RETURN;
        END

    IF @Role <> 'CongTy_Role'
        BEGIN
            RAISERROR(N'[STEP START] Ở Server CTY chỉ được tạo tài khoản Công Ty!', 16, 1);
            RETURN;
        END

    -------------------------------------------------------------
    -- 1. BẮT ĐẦU QUÁ TRÌNH
    -------------------------------------------------------------
    BEGIN TRY
        DECLARE @LoginName NVARCHAR(50) = N'cty_' + @UserName

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
        SET @SQL = N'USE CTY; CREATE USER [' + @LoginName + N'] FOR LOGIN [' + @LoginName + N'];';
        EXEC (@SQL);

        ---------------------------------------------------------
        SET @CurrentStep = N'A3 - ADD DATABASE ROLE';
        ---------------------------------------------------------
        SET @SQL =N'USE CTY; ALTER ROLE [' + @Role + N'] ADD MEMBER [' + @LoginName + N'];';
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

        ---------------------------------------------------------
        SET @CurrentStep = N'B1 - CALL CN1';
        ---------------------------------------------------------
        DECLARE @Ret1 INT;
        EXEC @Ret1 = [LINK_CN1].CN1.dbo.SP_TaoTaiKhoan_Receiver
                     @LoginName, @Password, @Role;

        IF @Ret1 <> 0
            BEGIN
                RAISERROR(N'[ERROR at STEP B1] Lỗi tạo tại CN1 - ReturnCode = %d', 16, 1, @Ret1);
                RETURN;
            END

        ---------------------------------------------------------
        SET @CurrentStep = N'B2 - CALL CN2';
        ---------------------------------------------------------
        DECLARE @Ret2 INT;
        EXEC @Ret2 = [LINK_CN2].CN2.dbo.SP_TaoTaiKhoan_Receiver
                     @LoginName, @Password, @Role;

        IF @Ret2 <> 0
            BEGIN
                RAISERROR(N'[ERROR at STEP B2] Lỗi tạo tại CN2 - ReturnCode = %d', 16, 1, @Ret2);
                RETURN;
            END


        ---------------------------------------------------------
        -- THÀNH CÔNG
        ---------------------------------------------------------
        PRINT N'Đã tạo tài khoản Công Ty trên toàn hệ thống.';
    END TRY

    -------------------------------------------------------------
    -- GLOBAL CATCH
    -------------------------------------------------------------
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(MAX) = ERROR_MESSAGE();
        RAISERROR(N'[ERROR at %s] %s', 16, 1, @CurrentStep, @ErrMsg);
    END CATCH
END
GO


GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_CongTy TO CongTy_Role;
GO

