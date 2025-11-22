USE CN2;
GO

EXEC dbo.SP_TaoTaiKhoan_ChiNhanh
     @Username = 'admin',
     @Password = 'Abcd@1234',
     @Role = 'ChiNhanh_Role';
GO
