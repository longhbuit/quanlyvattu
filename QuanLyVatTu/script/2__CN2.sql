USE CN2;   -- CN2 làm tương tự
GO

-- REPLICA của danh mục (tối thiểu)
CREATE TABLE NhanVien_REP (
                              MANV CHAR(10) PRIMARY KEY,
                              HoTen NVARCHAR(60) NOT NULL
);

CREATE TABLE Kho_REP (
                         MAKHO CHAR(4) PRIMARY KEY,
                         TenKho NVARCHAR(50)
);

CREATE TABLE Vattu_REP (
                           MAVT CHAR(4) PRIMARY KEY,
                           TenVT NVARCHAR(50),
                           DVT NVARCHAR(20)
);

-- Đơn đặt hàng
CREATE TABLE DatHang (
                         MADH INT IDENTITY PRIMARY KEY,
                         Ngay DATE NOT NULL DEFAULT GETDATE(),
                         NhaCC NVARCHAR(100),
                         MANV CHAR(10) NOT NULL,
                         MAKHO CHAR(4) NOT NULL,
                         TrangThai INT DEFAULT 0, -- 0: Chưa nhập; 1: Đã lập PN
                         MACN CHAR(2) NOT NULL DEFAULT 'CN1'
);

CREATE TABLE CTDDH (
                       MADH INT,
                       MAVT CHAR(4),
                       SoLuong INT CHECK (SoLuong > 0),
                       DonGia MONEY,
                       PRIMARY KEY (MADH, MAVT)
);

-- Phiếu nhập
CREATE TABLE PhieuNhap (
                           MAPN INT IDENTITY PRIMARY KEY,
                           Ngay DATE NOT NULL DEFAULT GETDATE(),
                           MADH INT NOT NULL,
                           MANV CHAR(10),
                           MAKHO CHAR(4),
                           MACN CHAR(2) DEFAULT 'CN1'
);

CREATE TABLE CTPN (
                      MAPN INT,
                      MAVT CHAR(4),
                      SoLuong INT CHECK (SoLuong > 0),
                      DonGia MONEY,
                      PRIMARY KEY (MAPN, MAVT)
);

-- Phiếu xuất
CREATE TABLE PhieuXuat (
                           MAPX INT IDENTITY PRIMARY KEY,
                           Ngay DATE DEFAULT GETDATE(),
                           MANV CHAR(10),
                           MAKHO CHAR(4),
                           MACN CHAR(2) DEFAULT 'CN1'
);

CREATE TABLE CTPX (
                      MAPX INT,
                      MAVT CHAR(4),
                      SoLuong INT CHECK (SoLuong > 0),
                      DonGia MONEY,
                      PRIMARY KEY (MAPX, MAVT)
);
