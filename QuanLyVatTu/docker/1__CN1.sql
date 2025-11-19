USE CN1
GO

-- A. SP CHÍNH: SP_TaoTaiKhoan_ChiNhanh
-- Mục đích: Chỉ nhóm Chi Nhánh (Trưởng phòng) mới được chạy.
GRANT EXECUTE ON SP_TaoTaiKhoan_ChiNhanh TO ChiNhanh_Role;

-- Cấm nhóm Công Ty (Giám đốc chỉ xem, không được tạo user chi nhánh tại đây)
DENY EXECUTE ON SP_TaoTaiKhoan_ChiNhanh TO CongTy_Role;
-- Cấm nhóm User thường
DENY EXECUTE ON SP_TaoTaiKhoan_ChiNhanh TO User_Role;


-- B. SP HỖ TRỢ: SP_TaoTaiKhoan_Receiver
-- Mục đích: Nhận lệnh từ Server CTY gửi xuống để tạo user nhân bản.
-- Ai gọi nó? -> Là Login từ Server CTY kết nối xuống.
GRANT EXECUTE ON SP_TaoTaiKhoan_Receiver TO PUBLIC;
GO