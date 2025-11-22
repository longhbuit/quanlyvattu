USE CTY;
GO

EXEC dbo.SP_TaoTaiKhoan_CongTy
     @UserName = 'admin',
     @Password = 'Abcd@1234',
     @Role = 'CongTy_Role';
GO 

USE CTY
GO