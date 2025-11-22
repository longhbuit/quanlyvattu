# 02_ERD_3_Site.md
# ERD CHI TIẾT 3 SITE – CSDL PHÂN TÁN QLVT

Tài liệu này mô tả **đầy đủ ERD** cho cả 3 site theo đúng phân mảnh:

- **Server3 – CTY**: danh mục dùng chung (phân mảnh dọc, full attributes)
- **Server1 – CN1**: giao dịch CN1 (phân mảnh ngang), bản sao tối thiểu
- **Server2 – CN2**: giao dịch CN2 (phân mảnh ngang), bản sao tối thiểu

Sơ đồ dùng Mermaid (chuẩn Markdown), phù hợp nộp báo cáo.

---

# 1. ERD SERVER3 – CTY (Danh mục đầy đủ)

Server3 chứa thông tin **NV, Kho, VT, ChiNhanh** theo đúng yêu cầu đề bài:
- CTY phải chứa toàn bộ danh mục để phục vụ tra cứu & báo cáo.
- Đây là **phân mảnh dọc** của hệ thống.

```mermaid
erDiagram

    ChiNhanh {
        nchar MACN PK
        nvarchar TenCN
        nvarchar DiaChi
        nvarchar SoDT
    }

    NhanVien {
        int MANV PK
        nvarchar HO
        nvarchar TEN
        nvarchar DIACHI
        datetime NGAYSINH
        float LUONG
        nchar MACN FK
    }

    Kho {
        nchar MAKHO PK
        nvarchar TENKHO
        nvarchar DIACHI
        nchar MACN FK
    }

    Vattu {
        nchar MAVT PK
        nvarchar TENVT
        nvarchar DVT
    }

    ChiNhanh ||--o{ NhanVien : "1-n"
    ChiNhanh ||--o{ Kho : "1-n"
```

---

# 2. ERD SERVER1 – CN1 (Giao dịch CN1 + Replica tối thiểu)

Server1 chứa:
- Các bảng giao dịch phát sinh bởi CN1 (**phân mảnh ngang**)
- Các bảng replica tối thiểu: chỉ lưu MANV / MAKHO / MAVT (**derived minimal replicas**)

```mermaid
erDiagram

    %% Replica tối thiểu
    NhanVien_CN1 {
        int MANV PK
    }

    Kho_CN1 {
        nchar MAKHO PK
    }

    Vattu_CN1 {
        nchar MAVT PK
    }

    %% Đơn đặt hàng
    DatHang_CN1 {
        nchar MasoDDH PK
        datetime Ngay
        nvarchar NhaCC
        int MANV FK
        nchar MAKHO FK
    }

    CTDDH_CN1 {
        nchar MasoDDH FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    %% Phiếu nhập
    PhieuNhap_CN1 {
        nchar MAPN PK
        datetime Ngay
        nchar MasoDDH FK
        int MANV FK
        nchar MAKHO FK
    }

    CTPN_CN1 {
        nchar MAPN FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    %% Phiếu xuất
    PhieuXuat_CN1 {
        nchar MAPX PK
        datetime Ngay
        nvarchar HoTenKH
        int MANV FK
        nchar MAKHO FK
    }

    CTPX_CN1 {
        nchar MAPX FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    DatHang_CN1 ||--o{ CTDDH_CN1 : "1-n"
    PhieuNhap_CN1 ||--o{ CTPN_CN1 : "1-n"
    PhieuXuat_CN1 ||--o{ CTPX_CN1 : "1-n"
```

---

# 3. ERD SERVER2 – CN2 (Giao dịch CN2 + Replica tối thiểu)

Cấu trúc **tương tự CN1**, nhưng chứa dữ liệu CN2.

```mermaid
erDiagram

    NhanVien_CN2 {
        int MANV PK
    }

    Kho_CN2 {
        nchar MAKHO PK
    }

    Vattu_CN2 {
        nchar MAVT PK
    }

    DatHang_CN2 {
        nchar MasoDDH PK
        datetime Ngay
        nvarchar NhaCC
        int MANV FK
        nchar MAKHO FK
    }

    CTDDH_CN2 {
        nchar MasoDDH FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    PhieuNhap_CN2 {
        nchar MAPN PK
        datetime Ngay
        nchar MasoDDH FK
        int MANV FK
        nchar MAKHO FK
    }

    CTPN_CN2 {
        nchar MAPN FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    PhieuXuat_CN2 {
        nchar MAPX PK
        datetime Ngay
        nvarchar HoTenKH
        int MANV FK
        nchar MAKHO FK
    }

    CTPX_CN2 {
        nchar MAPX FK
        nchar MAVT FK
        int SOLUONG
        float DONGIA
    }

    DatHang_CN2 ||--o{ CTDDH_CN2 : "1-n"
    PhieuNhap_CN2 ||--o{ CTPN_CN2 : "1-n"
    PhieuXuat_CN2 ||--o{ CTPX_CN2 : "1-n"
```

---

# 4. Sơ đồ so sánh 3 site

```mermaid
flowchart LR

    subgraph CTY["Server3 – Danh mục (Full)"]
        NV[NhanVien]
        KHO[Kho]
        VT[Vattu]
        CN[ChiNhanh]
    end

    subgraph CN1["Server1 – CN1 (Ngang + Replica)"]
        DH1[DH_CN1]
        PN1[PN_CN1]
        PX1[PX_CN1]
        R1_NV[Replica MANV]
        R1_KHO[Replica MAKHO]
        R1_VT[Replica MAVT]
    end

    subgraph CN2["Server2 – CN2 (Ngang + Replica)"]
        DH2[DH_CN2]
        PN2[PN_CN2]
        PX2[PX_CN2]
        R2_NV[Replica MANV]
        R2_KHO[Replica MAKHO]
        R2_VT[Replica MAVT]
    end

    NV --> R1_NV
    KHO --> R1_KHO
    VT --> R1_VT

    NV --> R2_NV
    KHO --> R2_KHO
    VT --> R2_VT
```

---

# 5. Kết luận

ERD 3 site đã được phân tách rõ ràng:

| Site | Bảng | Loại phân mảnh |
|------|------|----------------|
| **CTY** | Danh mục NV, Kho, VT, CN | Dọc – Full |
| **CN1** | PN, PX, ĐĐH CN1 | Ngang – CN1 |
| **CN2** | PN, PX, ĐĐH CN2 | Ngang – CN2 |
| **CN1/CN2** | NhanVien_CNx, Kho_CNx, Vattu_CNx | Replica tối thiểu |

Thiết kế đáp ứng:
- Đúng yêu cầu đề bài
- Dữ liệu nhất quán
- Tối ưu cho UI
- Phân quyền và báo cáo toàn công ty

---

*File này đã hoàn chỉnh, sẵn sàng nộp báo cáo.*

