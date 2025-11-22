USE CTY;
GO

-- Bảng Chi Nhánh
CREATE TABLE ChiNhanh
(
    MACN   CHAR(3) PRIMARY KEY,
    TenCN  NVARCHAR(100) NOT NULL,
    DiaChi NVARCHAR(200)
);

-- Bảng Nhân viên (dùng chung)
CREATE TABLE NhanVien
(
    MANV     CHAR(10) PRIMARY KEY,
    Ho       NVARCHAR(40),
    Ten      NVARCHAR(10),
    DiaChi   NVARCHAR(100),
    NgaySinh DATE,
    Luong    MONEY,
    MACN     CHAR(3) NOT NULL
        CHECK (MACN IN ('CN1', 'CN2'))
);

-- Bảng Kho (dùng chung)
CREATE TABLE Kho
(
    MAKHO  CHAR(4) PRIMARY KEY,
    TenKho NVARCHAR(50),
    DiaChi NVARCHAR(100),
    MACN   CHAR(3) NOT NULL
        CHECK (MACN IN ('CN1', 'CN2'))
);

-- Bảng Vật tư (dùng chung)
CREATE TABLE Vattu
(
    MAVT       CHAR(4) PRIMARY KEY,
    TenVT      NVARCHAR(50) NOT NULL,
    DVT        NVARCHAR(20),
    SOLUONGTON INT DEFAULT 0
);


