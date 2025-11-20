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

/* =======================================================
   PHẦN 3: STORED PROCEDURE TẠO TÀI KHOẢN (Logic Chính)
   ======================================================= */
IF OBJECT_ID('SP_TaoLogin_Global', 'P') IS NOT NULL
    DROP PROC SP_TaoLogin_Global
GO

CREATE PROCEDURE SP_TaoLogin_Global
    @LoginName VARCHAR(50),
    @Password VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    -- Kiểm tra trùng Login toàn hệ thống
    IF EXISTS (SELECT name FROM sys.server_principals WHERE name = @LoginName)
        RETURN 1 -- Trùng

    BEGIN TRY
        EXEC sp_addlogin @LoginName, @Password, 'CTY'
        -- Tạo User giữ chỗ (Optional, để sau này thống kê)
        EXEC sp_grantdbaccess @LoginName, @LoginName
        RETURN 0 -- Thành công
    END TRY
    BEGIN CATCH
        RETURN 1 -- Lỗi
    END CATCH
END
GO

GRANT EXECUTE ON SP_TaoLogin_Global TO PUBLIC
GO

IF OBJECT_ID('SP_TaoTaiKhoan_CongTy', 'P') IS NOT NULL
    DROP PROC SP_TaoTaiKhoan_CongTy
GO

CREATE PROCEDURE SP_TaoTaiKhoan_CongTy
    @LoginName VARCHAR(50), @Password VARCHAR(50), @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    -- SET XACT_ABORT ON; -- Bắt buộc cho giao dịch phân tán

    -- 1. KIỂM TRA QUYỀN (Chỉ CongTy_Role được chạy)
    IF IS_MEMBER('CongTy_Role') = 0 AND IS_SRVROLEMEMBER('sysadmin') = 0
        BEGIN
            RAISERROR(N'Chỉ nhóm Công Ty mới được dùng chức năng này!', 16, 1)
            RETURN
        END
    IF @Role <> 'CongTy_Role'
        BEGIN
            RAISERROR(N'Ở Server CTY chỉ được tạo tài khoản Công Ty!', 16, 1)
            RETURN
        END

    -- 2. GIAO DỊCH PHÂN TÁN
    -- BEGIN DISTRIBUTED TRANSACTION
        BEGIN TRY
            -- A. Tạo tại CTY (Local)
            IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = @LoginName)
                BEGIN
                    RAISERROR(N'Login không tồn tại!', 16, 1); ROLLBACK; RETURN;
                END

            EXEC sp_addrolemember @Role, @LoginName
            INSERT INTO TaiKhoan(LoginName) VALUES (@LoginName)

            -- B. Gọi sang Chi Nhánh 1 (Remote)
            DECLARE @Ret1 INT
            EXEC @Ret1 = [LINK_CN1].CN1.dbo.SP_TaoTaiKhoan_Receiver @LoginName, @Password, @Role
            IF @Ret1 <> 0 BEGIN RAISERROR(N'Lỗi tạo tại CN1', 16, 1); ROLLBACK; RETURN; END

            -- C. Gọi sang Chi Nhánh 2 (Remote)
            DECLARE @Ret2 INT
            EXEC @Ret2 = [LINK_CN2].CN2.dbo.SP_TaoTaiKhoan_Receiver @LoginName, @Password, @Role;
            IF @Ret2 <> 0 BEGIN RAISERROR(N'Lỗi tạo tại CN2', 16, 1); ROLLBACK; RETURN; END

            -- COMMIT TRANSACTION
            PRINT N'Đã tạo tài khoản Công Ty trên toàn hệ thống.'
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            DECLARE @Err NVARCHAR(MAX) = ERROR_MESSAGE();
            RAISERROR(@Err, 16, 1);
        END CATCH
END
GO



