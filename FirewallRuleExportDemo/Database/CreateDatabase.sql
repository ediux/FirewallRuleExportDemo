-- 防火牆規則匯出資料庫建立腳本

-- 建立資料庫
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FirewallDB')
BEGIN
    CREATE DATABASE FirewallDB;
END
GO

USE FirewallDB;
GO

-- 建立防火牆規則資料表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FIREWALL_RULE_SET]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FIREWALL_RULE_SET](
        [DEVICE_NAME] [nvarchar](100) NOT NULL,
        [RULENO] [nvarchar](100) NOT NULL,
        [UUID] [nvarchar](40) NOT NULL,
        [SAMEKEY] [int] NOT NULL,
        [INCOMING_INTERFACE] [nvarchar](100) NULL,
        [OUTGOING_INTERFACE] [nvarchar](100) NULL,
        [SOURCE_GROUP] [nvarchar](100) NULL,
        [SOURCE_IP_START] [nvarchar](100) NULL,
        [SOURCE_IP_END] [nvarchar](100) NULL,
        [SOURCE_FQDN] [nvarchar](500) NULL,
        [SOURCE_SUBNET] [nvarchar](100) NULL,
        [DESTINATION_GROUP] [nvarchar](100) NULL,
        [DESTINATION_IP_START] [nvarchar](100) NULL,
        [DESTINATION_IP_END] [nvarchar](100) NULL,
        [DESTINATION_FQDN] [nvarchar](500) NULL,
        [DESTINATION_SUBNET1] [nvarchar](100) NULL,
        [PROTOCAL] [nvarchar](100) NOT NULL,
        [PORT_S] [int] NULL,
        [PORT_E] [int] NULL,
        [SERVICE_GROUP] [nvarchar](100) NULL,
        [ACTION] [nvarchar](100) NOT NULL,
        [ENABLED] [bit] NOT NULL CONSTRAINT [DF_FIREWALL_RULE_SET_ENABLED] DEFAULT ((1)),
        [DATA_DT] [nvarchar](50) NULL,
        [HIT] [int] NULL,
        [FIRST_HIT_DATE] [nvarchar](50) NULL,
        [LAST_HIT_DATE] [nvarchar](50) NULL,
        [LayerObject] [nvarchar](50) NULL,
        [VDOM] [nvarchar](100) NULL,
        [HAS_EXPIRE_DATE] [bit] NULL,
        [EXPIRE_DATE] [nchar](10) NULL,
        [COMMENT] [nvarchar](500) NULL,
        CONSTRAINT [PK_FIREWALL_RULE_SET] PRIMARY KEY CLUSTERED 
        (
            [DEVICE_NAME] ASC,
            [RULENO] ASC,
            [UUID] ASC,
            [SAMEKEY] ASC
        )
    );
END
GO

-- 建立實體防火牆資訊資料表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FW_HW_INFO]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FW_HW_INFO](
        [BRAND] [nvarchar](100) NOT NULL,
        [MODEL] [nvarchar](100) NOT NULL,
        [FW_VERSION] [nvarchar](100) NOT NULL,
        [DEVICE_NAME] [nvarchar](100) NOT NULL,
        [DEVICE_IP] [nvarchar](100) NOT NULL,
        [DESCRIPTION] [nvarchar](1000) NULL,
        CONSTRAINT [PK_FW_HW_INFO] PRIMARY KEY CLUSTERED 
        (
            [BRAND] ASC,
            [MODEL] ASC,
            [FW_VERSION] ASC,
            [DEVICE_NAME] ASC
        )
    );
END
GO

-- 建立防火牆API資訊資料表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FW_API_INFO]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FW_API_INFO](
        [BRAND] [nvarchar](100) NOT NULL,
        [MODEL] [nvarchar](100) NOT NULL,
        [FW_VERSION] [nvarchar](100) NOT NULL,
        [SEQUENCE] [int] IDENTITY(1,1) NOT NULL,
        [API_NAME] [nvarchar](100) NOT NULL,
        [API_URL] [nvarchar](2000) NOT NULL,
        CONSTRAINT [PK_FW_API_INFO] PRIMARY KEY CLUSTERED 
        (
            [BRAND] ASC,
            [MODEL] ASC,
            [FW_VERSION] ASC,
            [SEQUENCE] ASC
        )
    );
END
GO

-- 建立IP群組主表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IP_GROUP_MAIN]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[IP_GROUP_MAIN](
        [IP_GROUP_ID] [int] IDENTITY(1,1) NOT NULL,
        [NAME] [nvarchar](100) NOT NULL,
        CONSTRAINT [PK_IP_GROUP_MAIN] PRIMARY KEY CLUSTERED 
        (
            [IP_GROUP_ID] ASC
        )
    );
END
GO

-- 建立IP群組明細表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[IP_GROUP_DETAILS]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[IP_GROUP_DETAILS](
        [IP_GROUP_DETAIL_ID] [int] IDENTITY(1,1) NOT NULL,
        [IP_GROUP_MAIN_ID] [int] NULL,
        [IPTYPE] [tinyint] NULL,
        [IPSTART] [nvarchar](50) NULL,
        [IPEND] [nvarchar](50) NULL,
        [IPSUBNET] [nvarchar](50) NULL,
        CONSTRAINT [PK_IP_GROUP_DETAILS] PRIMARY KEY CLUSTERED 
        (
            [IP_GROUP_DETAIL_ID] ASC
        ),
        CONSTRAINT [FK_IP_GROUP_DETAILS_MAIN] FOREIGN KEY([IP_GROUP_MAIN_ID])
        REFERENCES [dbo].[IP_GROUP_MAIN] ([IP_GROUP_ID])
    );
END
GO

-- 建立服務群組主表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SERVICE_GROUP_MAIN]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SERVICE_GROUP_MAIN](
        [SERVICE_GROUP_ID] [int] IDENTITY(1,1) NOT NULL,
        [NAME] [nvarchar](100) NOT NULL,
        CONSTRAINT [PK_SERVICE_GROUP_MAIN] PRIMARY KEY CLUSTERED 
        (
            [SERVICE_GROUP_ID] ASC
        )
    );
END
GO

-- 建立服務群組明細表
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SERVICE_GROUP_DETAILS]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SERVICE_GROUP_DETAILS](
        [SERVICE_GROUP_DETAIL_ID] [int] IDENTITY(1,1) NOT NULL,
        [SERVICE_GROUP_ID] [int] NULL,
        [PROTOCOL] [nvarchar](50) NULL,
        [PORT_S] [nvarchar](50) NULL,
        [PORT_E] [nvarchar](50) NULL,
        CONSTRAINT [PK_SERVICE_GROUP_DETAILS] PRIMARY KEY CLUSTERED 
        (
            [SERVICE_GROUP_DETAIL_ID] ASC
        ),
        CONSTRAINT [FK_SERVICE_GROUP_DETAILS_MAIN] FOREIGN KEY([SERVICE_GROUP_ID])
        REFERENCES [dbo].[SERVICE_GROUP_MAIN] ([SERVICE_GROUP_ID])
    );
END
GO

-- 插入示範資料
-- 防火牆裝置資訊
IF NOT EXISTS (SELECT * FROM FW_HW_INFO WHERE DEVICE_NAME = 'CheckPoint-FW-01')
BEGIN
    INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
    VALUES ('CheckPoint', 'R80.40', 'R80.40', 'CheckPoint-FW-01', '192.168.100.1', 'CheckPoint防火牆示範裝置');
END
GO

IF NOT EXISTS (SELECT * FROM FW_HW_INFO WHERE DEVICE_NAME = 'FortiGate-FW-01')
BEGIN
    INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
    VALUES ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'FortiGate-FW-01', '192.168.100.2', 'FortiGate防火牆示範裝置');
END
GO

-- 防火牆API資訊
IF NOT EXISTS (SELECT * FROM FW_API_INFO WHERE BRAND = 'CheckPoint' AND API_NAME = 'Login_Session')
BEGIN
    INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
    VALUES ('CheckPoint', 'R80.40', 'R80.40', 'Login_Session', 'https://192.168.100.1/web_api/login');
    
    INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
    VALUES ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallRules', 'https://192.168.100.1/web_api/show-access-rulebase');
    
    INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
    VALUES ('CheckPoint', 'R80.40', 'R80.40', 'Logout_Session', 'https://192.168.100.1/web_api/logout');
END
GO

IF NOT EXISTS (SELECT * FROM FW_API_INFO WHERE BRAND = 'FortiGate' AND API_NAME = 'Get_FirewallRules')
BEGIN
    INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
    VALUES ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallRules', 'https://192.168.100.2/api/v2/cmdb/firewall/policy');
END
GO

-- IP群組示範資料
IF NOT EXISTS (SELECT * FROM IP_GROUP_MAIN WHERE NAME = 'Internal-Network')
BEGIN
    INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Internal-Network');
    DECLARE @IPGroupID1 INT = SCOPE_IDENTITY();
    INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
    VALUES (@IPGroupID1, 3, NULL, NULL, '192.168.1.0/24');
END
GO

IF NOT EXISTS (SELECT * FROM IP_GROUP_MAIN WHERE NAME = 'Web-Servers')
BEGIN
    INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Web-Servers');
    DECLARE @IPGroupID2 INT = SCOPE_IDENTITY();
    INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
    VALUES (@IPGroupID2, 2, '10.0.0.10', '10.0.0.20', NULL);
END
GO

-- 服務群組示範資料
IF NOT EXISTS (SELECT * FROM SERVICE_GROUP_MAIN WHERE NAME = 'HTTP-Services')
BEGIN
    INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('HTTP-Services');
    DECLARE @ServiceGroupID1 INT = SCOPE_IDENTITY();
    INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
    VALUES (@ServiceGroupID1, 'TCP', '80', '80');
    INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
    VALUES (@ServiceGroupID1, 'TCP', '8080', '8080');
END
GO

PRINT '資料庫建立完成！';
GO
