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
USE CN2
GO

-- 1. Tạo bảng TaiKhoan (Để mapping giữa Login và Nhân viên)
IF OBJECT_ID('TaiKhoan', 'U') IS NOT NULL
    DROP TABLE TaiKhoan
GO

CREATE TABLE TaiKhoan (
                          LoginName VARCHAR(50) PRIMARY KEY,
                          MANV INT NOT NULL, -- Giả sử bạn đã có bảng NhanVien, nếu chưa thì bỏ FK
    -- FOREIGN KEY (MANV) REFERENCES NhanVien(MANV) 
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
IF OBJECT_ID('SP_TaoTaiKhoan_Receiver', 'P') IS NOT NULL
    DROP PROC SP_TaoTaiKhoan_Receiver
GO

CREATE PROCEDURE SP_TaoTaiKhoan_Receiver
    @LoginName VARCHAR(50),
    @Password VARCHAR(50),
    @MANV INT,
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Tạo Login nếu chưa có (Đồng bộ pass với CTY)
        IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = @LoginName)
            EXEC sp_addlogin @LoginName, @Password, 'CTY'

        -- Tạo User và Gán quyền
        EXEC sp_grantdbaccess @LoginName, @LoginName
        EXEC sp_addrolemember @Role, @LoginName

        -- Lưu vào bảng mapping (Bảng này phải có ở CN)
        -- INSERT INTO TaiKhoan(LoginName, MANV) VALUES (@LoginName, @MANV) 
        RETURN 0
    END TRY
    BEGIN CATCH
        RETURN 1
    END CATCH
END
GO
GRANT EXECUTE ON SP_TaoTaiKhoan_Receiver TO PUBLIC
GO

CREATE PROCEDURE SP_TaoTaiKhoan_ChiNhanh
    @LoginName VARCHAR(50), @Password VARCHAR(50),
    @MANV INT, @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1. KIỂM TRA QUYỀN
    IF IS_MEMBER('ChiNhanh_Role') = 0
        BEGIN
            RAISERROR(N'Bạn không có quyền thực hiện chức năng này!', 16, 1)
            RETURN
        END

    -- Chặn việc tạo User CongTy ở chi nhánh
    IF @Role = 'CongTy_Role'
        BEGIN
            RAISERROR(N'Chi nhánh không được tạo tài khoản Công Ty!', 16, 1)
            RETURN
        END

    -- 2. GIAO DỊCH PHÂN TÁN
    BEGIN DISTRIBUTED TRANSACTION
        BEGIN TRY
            -- A. Gọi lên CTY để tạo Login Global (Check trùng luôn)
            DECLARE @Ret INT
            -- [LINK_CTY] là Link Server trỏ về Cty
            EXEC @Ret = [LINK_CTY].CTY.dbo.SP_TaoLogin_Global @LoginName, @Password

            IF @Ret <> 0
                BEGIN
                    RAISERROR(N'Tên đăng nhập bị trùng hoặc lỗi tại Server CTY', 16, 1)
                    ROLLBACK TRANSACTION
                    RETURN
                END

            -- B. Tạo tại Chi Nhánh (Local)
            -- Tạo lại Login ở Local (vì bước A chỉ tạo ở Cty)
            EXEC sp_addlogin @LoginName, @Password, 'CTY'
            EXEC sp_grantdbaccess @LoginName, @LoginName
            EXEC sp_addrolemember @Role, @LoginName -- Role: ChiNhanh_Role hoặc User_Role
            INSERT INTO TaiKhoan(LoginName, MANV) VALUES (@LoginName, @MANV)

            COMMIT TRANSACTION
            PRINT N'Tạo tài khoản thành công!'
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            DECLARE @Err NVARCHAR(MAX) = ERROR_MESSAGE();
            RAISERROR(@Err, 16, 1);
        END CATCH
END
GO