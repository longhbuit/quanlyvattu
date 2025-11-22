USE master;
GO

-- LINK TỚI CHI NHÁNH 1 (CN1)
IF EXISTS (SELECT name FROM sys.servers WHERE name = 'LINK_CN1')
    EXEC sp_dropserver 'LINK_CN1', 'droplogins';

EXEC sp_addlinkedserver
     @server = 'LINK_CN1',
     @srvproduct = '', -- <--- ĐỂ TRỐNG (Quan trọng)
     @provider = 'SQLNCLI',
     @datasrc = 'mssql-cn1,1433'; -- Port của CN1

EXEC sp_addlinkedsrvlogin 'LINK_CN1', 'False', NULL, 'sa', 'YourStrong!Passw0rd';
EXEC sp_serveroption 'LINK_CN1', 'rpc out', 'true';
EXEC sp_serveroption 'LINK_CN1', 'rpc', 'true';
GO

-- LINK TỚI CHI NHÁNH 2 (CN2)
IF EXISTS (SELECT name FROM sys.servers WHERE name = 'LINK_CN2')
    EXEC sp_dropserver 'LINK_CN2', 'droplogins';

EXEC sp_addlinkedserver
     @server = 'LINK_CN2',
     @srvproduct = '', -- <--- ĐỂ TRỐNG
     @provider = 'SQLNCLI',
     @datasrc = 'mssql-cn2,1433'; -- Port của CN2

EXEC sp_addlinkedsrvlogin 'LINK_CN2', 'False', NULL, 'sa', 'YourStrong!Passw0rd';
EXEC sp_serveroption 'LINK_CN2', 'rpc out', 'true';
EXEC sp_serveroption 'LINK_CN2', 'rpc', 'true';
GO

-- Test query sang CN1
SELECT @@SERVERNAME AS [Server_Name_CN1] FROM [LINK_CN1].master.dbo.sysdatabases;

-- Test query sang CN2
SELECT @@SERVERNAME AS [Server_Name_CN2] FROM [LINK_CN2].master.dbo.sysdatabases;