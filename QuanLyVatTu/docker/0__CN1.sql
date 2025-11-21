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

    DECLARE @Step NVARCHAR(100) = N'INIT';

    ---------------------------------------------------------
    -- 1. KIỂM TRA QUYỀN
    ---------------------------------------------------------
    SET @Step = N'CHECK PERMISSION';
    IF IS_MEMBER('ChiNhanh_Role') = 0 AND IS_SRVROLEMEMBER('sysadmin') = 0
        BEGIN
            RAISERROR(N'[STEP: %s] Bạn không có quyền thực hiện chức năng này!', 16, 1, @Step);
            RETURN;
        END

    IF @Role = 'CongTy_Role'
        BEGIN
            RAISERROR(N'[STEP: %s] Chi nhánh không được tạo tài khoản Công Ty!', 16, 1, @Step);
            RETURN;
        END

    DECLARE @Ret INT;
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @Err NVARCHAR(MAX);

    ---------------------------------------------------------
    -- BƯỚC A: GỌI LÊN CTY
    ---------------------------------------------------------
    SET @Step = N'CALL REMOTE SP_TaoLogin_Global';

    BEGIN TRY
        EXEC @Ret = [LINK_CTY].CTY.dbo.SP_TaoLogin_Global @LoginName, @Password;
    END TRY
    BEGIN CATCH
        SET @Err = ERROR_MESSAGE();
        RAISERROR(N'[STEP: %s] Lỗi khi kết nối hoặc tạo Login tại Server CTY: %s',
            16, 1, @Step, @Err);
        RETURN;
    END CATCH

    IF @Ret <> 0
        BEGIN
            RAISERROR(N'[STEP: %s] Tên đăng nhập bị trùng tại CTY', 16, 1, @Step);
            RETURN;
        END


    ---------------------------------------------------------
    -- BƯỚC B: TẠO LOGIN LOCAL
    ---------------------------------------------------------
    SET @Step = N'CREATE LOCAL LOGIN';

    BEGIN TRY
        SET @SQL = 'CREATE LOGIN [' + @LoginName + '] WITH PASSWORD = ''' + @Password + '''';
        EXEC (@SQL);
    END TRY
    BEGIN CATCH
        SET @Err = ERROR_MESSAGE();
        RAISERROR(N'[STEP: %s] Lỗi tạo Login tại Chi Nhánh: %s',
            16, 1, @Step, @Err);
        RETURN;
    END CATCH


    ---------------------------------------------------------
    -- BƯỚC C: TRANSACTION TẠO USER
    ---------------------------------------------------------
    SET @Step = N'BEGIN LOCAL TRANSACTION';
    BEGIN TRANSACTION;

    BEGIN TRY

        SET @Step = N'CREATE USER';
        SET @SQL = 'CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + ']';
        EXEC (@SQL);

        SET @Step = N'ASSIGN ROLE';
        EXEC sp_addrolemember @Role, @LoginName;

        SET @Step = N'INSERT TaiKhoan';
        INSERT INTO TaiKhoan(LoginName) VALUES (@LoginName);

        COMMIT TRANSACTION;
        PRINT N'Tạo tài khoản hoàn tất thành công.';

    END TRY
    BEGIN CATCH

        ROLLBACK TRANSACTION;

        SET @Err = ERROR_MESSAGE();

        ---------------------------------------------------------
        -- CLEANUP
        ---------------------------------------------------------
        SET @Step = N'CLEANUP LOCAL LOGIN';
        SET @SQL = 'DROP LOGIN [' + @LoginName + ']';
        EXEC (@SQL);

        -- EXEC [LINK_CTY].CTY.dbo.SP_XoaLogin_Global @LoginName;

        RAISERROR(N'[STEP: %s] Lỗi khi thiết lập User/Data: %s. Đã hoàn tác toàn bộ.',
            16, 1, @Step, @Err);
    END CATCH

END
GO

GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_ChiNhanh TO ChiNhanh_Role;
GO
