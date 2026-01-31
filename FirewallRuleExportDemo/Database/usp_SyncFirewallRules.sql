-- Created by GitHub Copilot in SSMS - review carefully before executing

CREATE OR ALTER PROCEDURE dbo.usp_SyncFirewallRules
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 步驟1: 清空 FW_RULE_SET 並從 FIREWALL_RULE_SET 同步資料
        TRUNCATE TABLE dbo.FW_RULE_SET;
        
        -- 使用 CTE 來處理 SERVICE_GROUP 展開
        ;WITH ServiceGroupExpanded AS (
            SELECT 
                frs.DEVICE_NAME,
                frs.RULENO,
                frs.UUID,
                frs.INCOMING_INTERFACE,
                frs.OUTGOING_INTERFACE,
                frs.ENABLED,
                frs.ACTION,
                -- 來源 IP 類型判斷
                CASE 
                    WHEN frs.SOURCE_GROUP IS NOT NULL THEN 3  -- 群組
                    WHEN frs.SOURCE_FQDN IS NOT NULL THEN 2   -- FQDN
                    WHEN frs.SOURCE_IP_START IS NOT NULL OR frs.SOURCE_SUBNET IS NOT NULL THEN 1  -- IP/SUBNET/範圍
                    ELSE 9  -- 其他
                END AS SRC_TYPE,
                -- 來源 IP 值
                COALESCE(frs.SOURCE_GROUP, frs.SOURCE_FQDN, frs.SOURCE_IP_START, frs.SOURCE_SUBNET) AS SRC_IP,
                -- 目的 IP 類型判斷
                CASE 
                    WHEN frs.DESTINATION_GROUP IS NOT NULL THEN 3  -- 群組
                    WHEN frs.DESTINATION_FQDN IS NOT NULL THEN 2   -- FQDN
                    WHEN frs.DESTINATION_IP_START IS NOT NULL OR frs.DESTINATION_SUBNET1 IS NOT NULL THEN 1  -- IP/SUBNET/範圍
                    ELSE 9  -- 其他
                END AS DEST_TYPE,
                -- 目的 IP 值
                COALESCE(frs.DESTINATION_GROUP, frs.DESTINATION_FQDN, frs.DESTINATION_IP_START, frs.DESTINATION_SUBNET1) AS DEST_IP,
                -- 通訊協定和通訊埠處理
                CASE 
                    WHEN frs.SERVICE_GROUP IS NOT NULL THEN sgd.PROTOCOL
                    ELSE frs.PROTOCAL
                END AS PROTOCAL,
                CASE 
                    WHEN frs.SERVICE_GROUP IS NOT NULL THEN TRY_CAST(sgd.PORT_S AS INT)
                    ELSE frs.PORT_S
                END AS PORT_S,
                CASE 
                    WHEN frs.SERVICE_GROUP IS NOT NULL THEN TRY_CAST(sgd.PORT_E AS INT)
                    ELSE frs.PORT_E
                END AS PORT_E
            FROM dbo.FIREWALL_RULE_SET frs
            LEFT JOIN dbo.SERVICE_GROUP_MAIN sgm ON frs.SERVICE_GROUP = sgm.NAME
            LEFT JOIN dbo.SERVICE_GROUP_DETAILS sgd ON sgm.SERVICE_GROUP_ID = sgd.SERVICE_GROUP_ID
            WHERE frs.SERVICE_GROUP IS NULL OR sgd.SERVICE_GROUP_DETAIL_ID IS NOT NULL
        )
        INSERT INTO dbo.FW_RULE_SET (
            DEVICE_NAME,
            RULENO,
            UUID,
            SRC_TYPE,
            SRC_IP,
            DEST_TYPE,
            DEST_IP,
            PROTOCAL,
            PORT_S,
            PORT_E,
            ENABLED,
            INCOMING_INTERFACE,
            OUTGOING_INTERFACE,
            DIFF
        )
        SELECT 
            DEVICE_NAME,
            RULENO,
            UUID,
            SRC_TYPE,
            SRC_IP,
            DEST_TYPE,
            DEST_IP,
            PROTOCAL,
            PORT_S,
            PORT_E,
            ENABLED,
            INCOMING_INTERFACE,
            OUTGOING_INTERFACE,
            '0' AS DIFF  -- 初始值設為0
        FROM ServiceGroupExpanded;
        
        -- 步驟2: 清空 FW_RULE_RELATION
        TRUNCATE TABLE dbo.FW_RULE_RELATION;
        
        -- 步驟3: 比對 FW_RULE_SET 與 FW_RULE_USE，並寫入 FW_RULE_RELATION
        ;WITH SourceIPExpanded AS (
            -- 展開來源 IP 群組
            SELECT 
                rs.FW_RULE_SET_NO,
                rs.DEVICE_NAME,
                rs.SRC_TYPE,
                CASE 
                    WHEN rs.SRC_TYPE = 3 THEN  -- 群組
                        COALESCE(igd.IPSTART, igd.IPSUBNET)
                    ELSE rs.SRC_IP
                END AS SRC_IP_EXPANDED,
                rs.DEST_TYPE,
                rs.DEST_IP,
                rs.PROTOCAL,
                rs.PORT_S,
                rs.PORT_E
            FROM dbo.FW_RULE_SET rs
            LEFT JOIN dbo.IP_GROUP_MAIN igm ON rs.SRC_TYPE = 3 AND rs.SRC_IP = igm.NAME
            LEFT JOIN dbo.IP_GROUP_DETAILS igd ON igm.IP_GROUP_ID = igd.IP_GROUP_MAIN_ID
            WHERE rs.SRC_TYPE != 3 OR igd.IP_GROUP_DETAIL_ID IS NOT NULL
        ),
        FullExpanded AS (
            -- 展開目的 IP 群組
            SELECT 
                sie.FW_RULE_SET_NO,
                sie.DEVICE_NAME,
                sie.SRC_IP_EXPANDED,
                CASE 
                    WHEN sie.DEST_TYPE = 3 THEN  -- 群組
                        COALESCE(igd2.IPSTART, igd2.IPSUBNET)
                    ELSE sie.DEST_IP
                END AS DEST_IP_EXPANDED,
                sie.PROTOCAL,
                sie.PORT_S,
                sie.PORT_E
            FROM SourceIPExpanded sie
            LEFT JOIN dbo.IP_GROUP_MAIN igm2 ON sie.DEST_TYPE = 3 AND sie.DEST_IP = igm2.NAME
            LEFT JOIN dbo.IP_GROUP_DETAILS igd2 ON igm2.IP_GROUP_ID = igd2.IP_GROUP_MAIN_ID
            WHERE sie.DEST_TYPE != 3 OR igd2.IP_GROUP_DETAIL_ID IS NOT NULL
        )
        INSERT INTO dbo.FW_RULE_RELATION (
            FW_RULE_SET_NO,
            FW_RULE_USE_NO,
            MEMO,
            APPCODE,
            APPOWNER_EMPID
        )
        SELECT DISTINCT
            fe.FW_RULE_SET_NO,
            ru.FW_RULE_USE_NO,
            CASE 
                WHEN fe.SRC_IP_EXPANDED = ru.SRC_IP 
                     AND fe.DEST_IP_EXPANDED = ru.DEST_IP 
                     AND fe.PROTOCAL = ru.PROTOCAL 
                     AND COALESCE(fe.PORT_S, 0) = COALESCE(ru.PORT_S, 0)
                     AND COALESCE(fe.PORT_E, 0) = COALESCE(ru.PORT_E, 0)
                THEN N'規則完全匹配'
                ELSE N'規則部分匹配'
            END AS MEMO,
            ru.APPCODE,
            ru.OWNER_EMPID
        FROM FullExpanded fe
        INNER JOIN dbo.FW_RULE_USE ru ON 
            fe.DEVICE_NAME = ru.DEVICE_NAME
            AND (
                -- 來源IP匹配（支援精確匹配和模糊匹配）
                (fe.SRC_IP_EXPANDED = ru.SRC_IP 
                 OR fe.SRC_IP_EXPANDED LIKE '%' + ru.SRC_IP + '%' 
                 OR ru.SRC_IP LIKE '%' + fe.SRC_IP_EXPANDED + '%')
                -- 目的IP匹配（支援精確匹配和模糊匹配）
                AND (fe.DEST_IP_EXPANDED = ru.DEST_IP 
                     OR fe.DEST_IP_EXPANDED LIKE '%' + ru.DEST_IP + '%' 
                     OR ru.DEST_IP LIKE '%' + fe.DEST_IP_EXPANDED + '%')
                -- 通訊協定匹配
                AND fe.PROTOCAL = ru.PROTOCAL
                -- 通訊埠匹配 (考慮範圍)
                AND (
                    (COALESCE(ru.PORT_S, 0) BETWEEN COALESCE(fe.PORT_S, 0) AND COALESCE(fe.PORT_E, 999999))
                    OR (COALESCE(ru.PORT_E, 0) BETWEEN COALESCE(fe.PORT_S, 0) AND COALESCE(fe.PORT_E, 999999))
                    OR (COALESCE(fe.PORT_S, 0) BETWEEN COALESCE(ru.PORT_S, 0) AND COALESCE(ru.PORT_E, 999999))
                )
            )
        WHERE ru.ENABLED = 1;
        
        -- 步驟4: 更新 FW_RULE_SET 的 DIFF 欄位
        -- DIFF = 1: 有符合的規則
        UPDATE rs
        SET DIFF = '1'
        FROM dbo.FW_RULE_SET rs
        WHERE EXISTS (
            SELECT 1 
            FROM dbo.FW_RULE_RELATION rel 
            WHERE rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
        );
        
        -- DIFF = 2: 沒有符合的規則
        UPDATE dbo.FW_RULE_SET
        SET DIFF = '2'
        WHERE DIFF = '0';
        
        COMMIT TRANSACTION;
        
        -- 返回統計資訊
        SELECT 
            '同步完成' AS Status,
            (SELECT COUNT(*) FROM dbo.FW_RULE_SET) AS TotalRules,
            (SELECT COUNT(*) FROM dbo.FW_RULE_SET WHERE DIFF = '1') AS MatchedRules,
            (SELECT COUNT(*) FROM dbo.FW_RULE_SET WHERE DIFF = '2') AS UnmatchedRules,
            (SELECT COUNT(*) FROM dbo.FW_RULE_RELATION) AS TotalRelations,
            (SELECT COUNT(DISTINCT FW_RULE_SET_NO) FROM dbo.FW_RULE_RELATION) AS UniqueMatchedRules;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO