USE CTY
GO

-- A. SP CHÍNH: SP_TaoTaiKhoan_CongTy
-- Mục đích: Chỉ nhóm Công Ty (Giám đốc) mới được chạy để tạo user CTY.
GRANT EXECUTE ON dbo.SP_TaoTaiKhoan_CongTy TO CongTy_Role;

-- Cấm tuyệt đối nhóm Chi Nhánh và User (nếu họ lỡ login vào server này)
DENY EXECUTE ON dbo.SP_TaoTaiKhoan_CongTy TO ChiNhanh_Role;
DENY EXECUTE ON dbo.SP_TaoTaiKhoan_CongTy TO User_Role;


-- B. SP HỖ TRỢ: SP_TaoLogin_Global
-- Mục đích: Nhận lệnh từ các Chi nhánh gửi lên.
-- Ai gọi nó? -> Là cái "Login" được cấu hình trong Linked Server ở dưới Chi nhánh.
-- Trong đồ án, để đơn giản và tránh lỗi kết nối từ xa, ta thường cấp cho PUBLIC
-- (Hoặc tốt hơn là cấp cho đúng cái User mà Link Server dùng để kết nối)
GRANT EXECUTE ON dbo.SP_TaoLogin_Global TO PUBLIC;
GO