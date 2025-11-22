USE CN1;
GO

EXEC dbo.SP_TaoTaiKhoan_ChiNhanh
     @Username = 'admin',
     @Password = 'Abcd@1234',
     @Role = 'ChiNhanh_Role';
GO

EXEC LINK_CTY.CTY.dbo.SP_TaoLogin_Global
     @LoginName = 'cn1_admin',
     @Role = 'ChiNhanh_Role';

