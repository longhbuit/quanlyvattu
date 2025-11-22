USE master;
GO

-- LINK VỀ CÔNG TY (CTY)
IF EXISTS (SELECT name FROM sys.servers WHERE name = 'LINK_CTY')
    EXEC sp_dropserver 'LINK_CTY', 'droplogins';

EXEC sp_addlinkedserver 
    @server = 'LINK_CTY', 
    @srvproduct = '', 
    @provider = 'SQLNCLI', 
    @datasrc = 'mssql-cty,1433'; -- Port của CTY

EXEC sp_addlinkedsrvlogin 'LINK_CTY', 'False', NULL, 'sa', 'YourStrong!Passw0rd';
EXEC sp_serveroption 'LINK_CTY', 'rpc out', 'true';
EXEC sp_serveroption 'LINK_CTY', 'rpc', 'true';
GO

-- LINK SANG CHI NHÁNH 2 (CN2)
IF EXISTS (SELECT name FROM sys.servers WHERE name = 'LINK_CN2')
    EXEC sp_dropserver 'LINK_CN2', 'droplogins';

EXEC sp_addlinkedserver 
    @server = 'LINK_CN2', 
    @srvproduct = '', 
    @provider = 'SQLNCLI', 
    @datasrc = 'mssql-cn2,1433'; -- Port của CN2

EXEC sp_addlinkedsrvlogin 'LINK_CN2', 'False', NULL, 'sa', 'YourStrong!Passw0rd';
EXEC sp_serveroption 'LINK_CN2', 'rpc out', 'true';
EXEC sp_serveroption 'LINK_CN2', 'rpc', 'true';
GO