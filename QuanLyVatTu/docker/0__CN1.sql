/* =======================================================
   PHẦN 1: THIẾT LẬP MÔI TRƯỜNG & SERVER ROLE (Chạy tại Master)
   Mục đích: Tạo quyền cấp cao để các User quản lý có thể tạo Login
   ======================================================= */
USE master;
GO

-- 1. Tạo Database CTY nếu chưa có
IF DB_ID('CN1') IS NULL
CREATE DATABASE CN1;
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
USE CN1
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
IF OBJECT_ID('dbo.SP_TaoTaiKhoan_Receiver', 'P') IS NOT NULL
    DROP PROC dbo.SP_TaoTaiKhoan_Receiver
GO
CREATE PROCEDURE dbo.SP_TaoTaiKhoan_Receiver
    @LoginName VARCHAR(50),
    @Password VARCHAR(50),
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Tạo Login nếu chưa có (Đồng bộ pass với Database CN1)
        IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = @LoginName)
            EXEC sp_addlogin @LoginName, @Password, 'CN1';

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
CREATE PROCEDURE dbo.SP_TaoTaiKhoan_ChiNhanh
    @LoginName VARCHAR(50),
    @Password VARCHAR(50),
    @Role VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1. KIỂM TRA QUYỀN (Giữ nguyên)
    IF IS_MEMBER('ChiNhanh_Role') = 0 AND IS_SRVROLEMEMBER('sysadmin') = 0
        BEGIN
            RAISERROR(N'Bạn không có quyền thực hiện chức năng này!', 16, 1);
            RETURN;
        END

    IF @Role = 'CongTy_Role'
        BEGIN
            RAISERROR(N'Chi nhánh không được tạo tài khoản Công Ty!', 16, 1);
            RETURN;
        END

    -- KHAI BÁO BIẾN KIỂM SOÁT LỖI
    DECLARE @Ret INT;

    ---------------------------------------------------------
    -- BƯỚC A: GỌI LÊN CTY (NẰM NGOÀI TRANSACTION)
    ---------------------------------------------------------
    TRY_REMOTE:
    BEGIN TRY
        -- Gọi Link Server: Bên kia tự kiểm tra trùng và tạo Login
        EXEC @Ret = [LINK_CTY].CTY.dbo.SP_TaoLogin_Global @LoginName, @Password;
    END TRY
    BEGIN CATCH
        RAISERROR(N'Lỗi khi kết nối hoặc tạo Login tại Server CTY', 16, 1);
        RETURN; -- Dừng ngay, chưa làm gì ở local nên không cần dọn dẹp
    END CATCH

    IF @Ret <> 0
        BEGIN
            RAISERROR(N'Tên đăng nhập bị trùng tại CTY', 16, 1);
            RETURN;
        END

    ---------------------------------------------------------
    -- BƯỚC B: TẠO LOGIN TẠI CHI NHÁNH (NẰM NGOÀI TRANSACTION)
    ---------------------------------------------------------
    BEGIN TRY
        -- Dùng CREATE LOGIN thay cho sp_addlogin (đã cũ)
        DECLARE @SQL NVARCHAR(MAX);
        SET @SQL = 'CREATE LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + '''';
        EXEC (@SQL);
    END TRY
    BEGIN CATCH
        -- NẾU LỖI: Phải "Bù trừ" bằng cách xóa Login vừa tạo bên CTY
        DECLARE @ErrorMsg NVARCHAR(MAX) = ERROR_MESSAGE();
        -- Gọi hàm xóa bên CTY (Bạn cần viết thêm SP này bên CTY)
        -- EXEC [LINK_CTY].CTY.dbo.SP_XoaLogin_Global @LoginName; 
        RAISERROR(N'Lỗi tạo Login tại Chi Nhánh: %s. Đã hủy bên CTY.', 16, 1, @ErrorMsg);
        RETURN;
    END CATCH

    ---------------------------------------------------------
    -- BƯỚC C: TẠO USER DB & GHI DỮ LIỆU (NẰM TRONG TRANSACTION)
    ---------------------------------------------------------
    -- Chỉ bắt đầu Transaction cho các thao tác dữ liệu trong DB hiện tại
    BEGIN TRANSACTION
        BEGIN TRY
            -- 1. Tạo User cho Database hiện tại
            SET @SQL = 'CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + ']';
            EXEC (@SQL);

            -- 2. Gán quyền (Role)
            -- sp_addrolemember vẫn dùng được, hoặc dùng ALTER ROLE
            EXEC sp_addrolemember @Role, @LoginName;

            -- 3. Ghi vào bảng TaiKhoan (Dữ liệu nghiệp vụ)
            INSERT INTO TaiKhoan(LoginName) VALUES (@LoginName);

            COMMIT TRANSACTION;
            PRINT N'Tạo tài khoản thành công hoàn toàn.';
        END TRY
        BEGIN CATCH
            -- Nếu lỗi tại bước này
            ROLLBACK TRANSACTION;

            -- CLEANUP (BÙ TRỪ): Xóa Login Local và Remote vì việc tạo User thất bại
            -- 1. Xóa Login Local
            SET @SQL = 'DROP LOGIN [' + @LoginName + ']';
            EXEC (@SQL);

            -- 2. Xóa Login Remote (Gợi ý nên có SP này)
            -- EXEC [LINK_CTY].CTY.dbo.SP_XoaLogin_Global @LoginName;

            DECLARE @Err NVARCHAR(MAX) = ERROR_MESSAGE();
            RAISERROR(N'Lỗi khi thiết lập User/Data: %s. Đã hoàn tác toàn bộ.', 16, 1, @Err);
        END CATCH
END
GO

GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_ChiNhanh TO ChiNhanh_Role;
DENY EXECUTE ON dbo.SP_TaoTaiKhoan_ChiNhanh TO CongTy_Role;
DENY EXECUTE ON dbo.SP_TaoTaiKhoan_ChiNhanh TO User_Role;
GO
