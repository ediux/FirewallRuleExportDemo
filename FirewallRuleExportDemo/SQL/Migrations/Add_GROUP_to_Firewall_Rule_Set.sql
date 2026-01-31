/*
CMDB 的部署指令碼

此程式碼是由工具所產生。
變更此檔案可能會導致不正確的行為，而且如果發生以下狀況將遺失:
程式碼已重新產生。
*/

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
:setvar DatabaseName "CMDB"
:setvar DefaultFilePrefix "CMDB"
:setvar DefaultDataPath "E:\MSSQLServer\MSSQL16.MSSQLSERVER\MSSQL\DATA\"
:setvar DefaultLogPath "E:\MSSQLServer\MSSQL16.MSSQLSERVER\MSSQL\DATA\"

GO
:on error exit
GO
/*
如果不支援 SQLCMD 模式，則偵測 SQLCMD 模式並停用指令碼執行。
若要在啟用 SQLCMD 模式後重新啟用指令碼，請執行下列動作:
將 NOEXEC 設定為關閉; 
*/
:setvar __IsSqlCmdEnabled "True"
GO
IF N'$(__IsSqlCmdEnabled)' NOT LIKE N'True'
    BEGIN
        PRINT N'必須啟用 SQLCMD 模式才能成功執行此指令碼。';
        SET NOEXEC ON;
    END


GO
USE [$(DatabaseName)];


GO

IF (SELECT OBJECT_ID('tempdb..#tmpErrors')) IS NOT NULL DROP TABLE #tmpErrors
GO
CREATE TABLE #tmpErrors (Error int)
GO
SET XACT_ABORT ON
GO
SET TRANSACTION ISOLATION LEVEL READ COMMITTED
GO
BEGIN TRANSACTION
GO
PRINT N'正在置放 預設條件約束 [dbo].[DF_FIREWALL_RULE_SET_ENABLED]...';


GO
ALTER TABLE [dbo].[FIREWALL_RULE_SET] DROP CONSTRAINT [DF_FIREWALL_RULE_SET_ENABLED];


GO
IF @@ERROR <> 0
   AND @@TRANCOUNT > 0
    BEGIN
        ROLLBACK;
    END

IF OBJECT_ID(N'tempdb..#tmpErrors') IS NULL
    CREATE TABLE [#tmpErrors] (
        Error INT
    );

IF @@TRANCOUNT = 0
    BEGIN
        INSERT  INTO #tmpErrors (Error)
        VALUES                 (1);
        BEGIN TRANSACTION;
    END


GO
/*
必須加入資料表 [dbo].[FIREWALL_RULE_SET] 的資料行 [dbo].[FIREWALL_RULE_SET].[ACTION]，但該資料行沒有預設值而且不允許 NULL 值。如果資料表包含資料，則 ALTER 指令碼將無法運作。若要避免這個問題，您必須: 在資料行加入預設值、將它標示為允許 NULL 值，或啟用產生智慧型預設值做為部署選項。
*/
GO
PRINT N'開始重建資料表 [dbo].[FIREWALL_RULE_SET]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_FIREWALL_RULE_SET] (
    [DEVICE_NAME]          NVARCHAR (100) NOT NULL,
    [RULENO]               NVARCHAR (100) NOT NULL,
    [UUID]                 NVARCHAR (40)  NOT NULL,
    [SAMEKEY]              INT            NOT NULL,
    [SOURCE_GROUP]         NVARCHAR (100) NULL,
    [SOURCE_IP_START]      NVARCHAR (100) NULL,
    [SOURCE_IP_END]        NVARCHAR (100) NULL,
    [SOURCE_FQDN]          NVARCHAR (500) NULL,
    [SOURCE_SUBNET]        NVARCHAR (100) NULL,
    [DESTINATION_GROUP]    NVARCHAR (100) NULL,
    [DESTINATION_IP_START] NVARCHAR (100) NULL,
    [DESTINATION_IP_END]   NVARCHAR (100) NULL,
    [DESTINATION_FQDN]     NVARCHAR (500) NULL,
    [DESTINATION_SUBNET1]  NVARCHAR (100) NULL,
    [PROTOCAL]             NVARCHAR (100) NOT NULL,
    [PORT_S]               INT            NULL,
    [PORT_E]               INT            NULL,
    [SERVICE_GROUP]        NVARCHAR (100) NULL,
    [ACTION]               NVARCHAR (100) NOT NULL,
    [ENABLED]              BIT            CONSTRAINT [DF_FIREWALL_RULE_SET_ENABLED] DEFAULT ((1)) NOT NULL,
    [DATA_DT]              NVARCHAR (50)  NULL,
    [HIT]                  INT            NULL,
    [FIRST_HIT_DATE]       NVARCHAR (50)  NULL,
    [LAST_HIT_DATE]        NVARCHAR (50)  NULL,
    [LayerObject]          NVARCHAR (50)  NULL,
    [VDOM]                 NVARCHAR (100) NULL,
    [HAS_EXPIRE_DATE]      BIT            NULL,
    [EXPIRE_DATE]          NCHAR (10)     NULL,
    [COMMENT]              NVARCHAR (500) NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_FIREWALL_RULE_SET1] PRIMARY KEY CLUSTERED ([DEVICE_NAME] ASC, [RULENO] ASC, [UUID] ASC, [SAMEKEY] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[FIREWALL_RULE_SET])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_FIREWALL_RULE_SET] ([DEVICE_NAME], [RULENO], [UUID], [SAMEKEY], [SOURCE_IP_START], [SOURCE_IP_END], [SOURCE_FQDN], [SOURCE_SUBNET], [DESTINATION_IP_START], [DESTINATION_IP_END], [DESTINATION_FQDN], [DESTINATION_SUBNET1], [PROTOCAL], [PORT_S], [PORT_E], [ENABLED], [DATA_DT], [HIT], [FIRST_HIT_DATE], [LAST_HIT_DATE], [LayerObject], [VDOM], [HAS_EXPIRE_DATE], [EXPIRE_DATE], [COMMENT])
        SELECT   [DEVICE_NAME],
                 [RULENO],
                 [UUID],
                 [SAMEKEY],
                 [SOURCE_IP_START],
                 [SOURCE_IP_END],
                 [SOURCE_FQDN],
                 [SOURCE_SUBNET],
                 [DESTINATION_IP_START],
                 [DESTINATION_IP_END],
                 [DESTINATION_FQDN],
                 [DESTINATION_SUBNET1],
                 [PROTOCAL],
                 [PORT_S],
                 [PORT_E],
                 [ENABLED],
                 [DATA_DT],
                 [HIT],
                 [FIRST_HIT_DATE],
                 [LAST_HIT_DATE],
                 [LayerObject],
                 [VDOM],
                 [HAS_EXPIRE_DATE],
                 [EXPIRE_DATE],
                 [COMMENT]
        FROM     [dbo].[FIREWALL_RULE_SET]
        ORDER BY [DEVICE_NAME] ASC, [RULENO] ASC, [UUID] ASC, [SAMEKEY] ASC;
    END

DROP TABLE [dbo].[FIREWALL_RULE_SET];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_FIREWALL_RULE_SET]', N'FIREWALL_RULE_SET';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_constraint_PK_FIREWALL_RULE_SET1]', N'PK_FIREWALL_RULE_SET', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
IF @@ERROR <> 0
   AND @@TRANCOUNT > 0
    BEGIN
        ROLLBACK;
    END

IF OBJECT_ID(N'tempdb..#tmpErrors') IS NULL
    CREATE TABLE [#tmpErrors] (
        Error INT
    );

IF @@TRANCOUNT = 0
    BEGIN
        INSERT  INTO #tmpErrors (Error)
        VALUES                 (1);
        BEGIN TRANSACTION;
    END


GO

IF EXISTS (SELECT * FROM #tmpErrors) ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT>0 BEGIN
PRINT N'資料庫更新的異動部分成功。'
COMMIT TRANSACTION
END
ELSE PRINT N'資料庫更新的易動部分失敗。'
GO
IF (SELECT OBJECT_ID('tempdb..#tmpErrors')) IS NOT NULL DROP TABLE #tmpErrors
GO
GO
PRINT N'更新完成。';


GO
