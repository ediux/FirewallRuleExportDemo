-- 防火牆規則匯出系統 - 資料查詢與驗證腳本
-- Firewall Rule Export System - Data Query and Verification Script
-- ================================================================

USE FirewallDB;
GO

PRINT '========================================';
PRINT '防火牆規則匯出系統 - 資料查詢';
PRINT 'Firewall Rule Export System - Data Query';
PRINT '========================================';
PRINT '';

-- ================================================================
-- 1. 資料統計 / Data Statistics
-- ================================================================
PRINT '-- 1. 資料統計 / Data Statistics';
PRINT '========================================';

SELECT 
    '防火牆硬體資訊 / Firewall Hardware' as TableName,
    COUNT(*) as RecordCount
FROM FW_HW_INFO
UNION ALL
SELECT 
    '防火牆API資訊 / Firewall API Info' as TableName,
    COUNT(*) as RecordCount
FROM FW_API_INFO
UNION ALL
SELECT 
    'IP群組主表 / IP Group Main' as TableName,
    COUNT(*) as RecordCount
FROM IP_GROUP_MAIN
UNION ALL
SELECT 
    'IP群組明細 / IP Group Details' as TableName,
    COUNT(*) as RecordCount
FROM IP_GROUP_DETAILS
UNION ALL
SELECT 
    '服務群組主表 / Service Group Main' as TableName,
    COUNT(*) as RecordCount
FROM SERVICE_GROUP_MAIN
UNION ALL
SELECT 
    '服務群組明細 / Service Group Details' as TableName,
    COUNT(*) as RecordCount
FROM SERVICE_GROUP_DETAILS
UNION ALL
SELECT 
    '防火牆規則 / Firewall Rules' as TableName,
    COUNT(*) as RecordCount
FROM FIREWALL_RULE_SET;

PRINT '';
PRINT '';

-- ================================================================
-- 2. 防火牆裝置清單 / Firewall Device List
-- ================================================================
PRINT '-- 2. 防火牆裝置清單 / Firewall Device List';
PRINT '========================================';

SELECT 
    BRAND as 廠牌,
    MODEL as 型號,
    FW_VERSION as 韌體版本,
    DEVICE_NAME as 裝置名稱,
    DEVICE_IP as IP位址,
    DESCRIPTION as 說明
FROM FW_HW_INFO
ORDER BY BRAND, DEVICE_NAME;

PRINT '';
PRINT '';

-- ================================================================
-- 3. 各防火牆的API設定數量 / API Configuration Count by Firewall
-- ================================================================
PRINT '-- 3. 各防火牆的API設定數量 / API Configuration Count by Firewall';
PRINT '========================================';

SELECT 
    BRAND as 廠牌,
    MODEL as 型號,
    FW_VERSION as 韌體版本,
    COUNT(*) as API設定數量
FROM FW_API_INFO
GROUP BY BRAND, MODEL, FW_VERSION
ORDER BY BRAND, MODEL;

PRINT '';
PRINT '';

-- ================================================================
-- 4. IP群組列表及成員數量 / IP Group List with Member Count
-- ================================================================
PRINT '-- 4. IP群組列表及成員數量 / IP Group List with Member Count';
PRINT '========================================';

SELECT 
    m.NAME as 群組名稱,
    COUNT(d.IP_GROUP_DETAIL_ID) as 成員數量,
    SUM(CASE WHEN d.IPTYPE = 1 THEN 1 ELSE 0 END) as 單一IP數,
    SUM(CASE WHEN d.IPTYPE = 2 THEN 1 ELSE 0 END) as IP範圍數,
    SUM(CASE WHEN d.IPTYPE = 3 THEN 1 ELSE 0 END) as 子網路數
FROM IP_GROUP_MAIN m
LEFT JOIN IP_GROUP_DETAILS d ON m.IP_GROUP_ID = d.IP_GROUP_MAIN_ID
GROUP BY m.NAME, m.IP_GROUP_ID
ORDER BY m.NAME;

PRINT '';
PRINT '';

-- ================================================================
-- 5. IP群組明細資料 / IP Group Details
-- ================================================================
PRINT '-- 5. IP群組明細資料範例 (前20筆) / IP Group Details Sample (Top 20)';
PRINT '========================================';

SELECT TOP 20
    m.NAME as 群組名稱,
    CASE d.IPTYPE
        WHEN 1 THEN '單一IP'
        WHEN 2 THEN 'IP範圍'
        WHEN 3 THEN '子網路'
        ELSE '未知'
    END as IP類型,
    ISNULL(d.IPSTART, '-') as 起始IP,
    ISNULL(d.IPEND, '-') as 結束IP,
    ISNULL(d.IPSUBNET, '-') as 子網路
FROM IP_GROUP_MAIN m
INNER JOIN IP_GROUP_DETAILS d ON m.IP_GROUP_ID = d.IP_GROUP_MAIN_ID
ORDER BY m.NAME, d.IPTYPE;

PRINT '';
PRINT '';

-- ================================================================
-- 6. 服務群組列表及成員數量 / Service Group List with Member Count
-- ================================================================
PRINT '-- 6. 服務群組列表及成員數量 / Service Group List with Member Count';
PRINT '========================================';

SELECT 
    m.NAME as 群組名稱,
    COUNT(d.SERVICE_GROUP_DETAIL_ID) as 服務數量,
    STRING_AGG(d.PROTOCOL, ', ') as 使用協定
FROM SERVICE_GROUP_MAIN m
LEFT JOIN SERVICE_GROUP_DETAILS d ON m.SERVICE_GROUP_ID = d.SERVICE_GROUP_ID
GROUP BY m.NAME, m.SERVICE_GROUP_ID
ORDER BY m.NAME;

PRINT '';
PRINT '';

-- ================================================================
-- 7. 服務群組明細資料 / Service Group Details
-- ================================================================
PRINT '-- 7. 服務群組明細資料範例 (前20筆) / Service Group Details Sample (Top 20)';
PRINT '========================================';

SELECT TOP 20
    m.NAME as 群組名稱,
    d.PROTOCOL as 協定,
    d.PORT_S as 起始埠,
    d.PORT_E as 結束埠,
    CASE 
        WHEN d.PORT_S = d.PORT_E THEN d.PORT_S
        ELSE d.PORT_S + '-' + d.PORT_E
    END as 埠號範圍
FROM SERVICE_GROUP_MAIN m
INNER JOIN SERVICE_GROUP_DETAILS d ON m.SERVICE_GROUP_ID = d.SERVICE_GROUP_ID
ORDER BY m.NAME, d.PROTOCOL, d.PORT_S;

PRINT '';
PRINT '';

-- ================================================================
-- 8. 防火牆規則統計 / Firewall Rules Statistics
-- ================================================================
PRINT '-- 8. 防火牆規則統計 / Firewall Rules Statistics';
PRINT '========================================';

SELECT 
    DEVICE_NAME as 防火牆名稱,
    COUNT(*) as 規則總數,
    SUM(CASE WHEN ENABLED = 1 THEN 1 ELSE 0 END) as 啟用規則數,
    SUM(CASE WHEN ENABLED = 0 THEN 1 ELSE 0 END) as 停用規則數,
    SUM(CASE WHEN ACTION = 'Accept' OR ACTION = 'accept' THEN 1 ELSE 0 END) as 允許規則數,
    SUM(CASE WHEN ACTION NOT IN ('Accept', 'accept') THEN 1 ELSE 0 END) as 拒絕規則數,
    SUM(ISNULL(HIT, 0)) as 總命中次數,
    MAX(DATA_DT) as 最後更新日期
FROM FIREWALL_RULE_SET
GROUP BY DEVICE_NAME
ORDER BY DEVICE_NAME;

PRINT '';
PRINT '';

-- ================================================================
-- 9. 防火牆規則詳細列表 / Detailed Firewall Rules List
-- ================================================================
PRINT '-- 9. 防火牆規則詳細列表 (CheckPoint-FW-01) / Detailed Rules (CheckPoint-FW-01)';
PRINT '========================================';

SELECT 
    RULENO as 規則編號,
    ISNULL(SOURCE_GROUP, 
        CASE 
            WHEN SOURCE_IP_START IS NOT NULL THEN SOURCE_IP_START + 
                CASE WHEN SOURCE_IP_END != SOURCE_IP_START THEN '-' + SOURCE_IP_END ELSE '' END
            WHEN SOURCE_SUBNET IS NOT NULL THEN SOURCE_SUBNET
            ELSE 'Any'
        END) as 來源,
    ISNULL(DESTINATION_GROUP,
        CASE 
            WHEN DESTINATION_IP_START IS NOT NULL THEN DESTINATION_IP_START + 
                CASE WHEN DESTINATION_IP_END != DESTINATION_IP_START THEN '-' + DESTINATION_IP_END ELSE '' END
            WHEN DESTINATION_SUBNET1 IS NOT NULL THEN DESTINATION_SUBNET1
            ELSE 'Any'
        END) as 目的,
    ISNULL(SERVICE_GROUP,
        CASE 
            WHEN PORT_S IS NOT NULL THEN PROTOCAL + '/' + 
                CAST(PORT_S AS VARCHAR) + 
                CASE WHEN PORT_E != PORT_S THEN '-' + CAST(PORT_E AS VARCHAR) ELSE '' END
            WHEN PROTOCAL IS NOT NULL THEN PROTOCAL
            ELSE 'Any'
        END) as 服務,
    ACTION as 動作,
    CASE WHEN ENABLED = 1 THEN '是' ELSE '否' END as 啟用,
    ISNULL(HIT, 0) as 命中次數,
    COMMENT as 備註
FROM FIREWALL_RULE_SET
WHERE DEVICE_NAME = 'CheckPoint-FW-01'
ORDER BY CAST(RULENO AS INT);

PRINT '';
PRINT '';

-- ================================================================
-- 10. 防火牆規則詳細列表 / Detailed Firewall Rules List
-- ================================================================
PRINT '-- 10. 防火牆規則詳細列表 (FortiGate-FW-01) / Detailed Rules (FortiGate-FW-01)';
PRINT '========================================';

SELECT 
    RULENO as 規則編號,
    INCOMING_INTERFACE as 來源介面,
    OUTGOING_INTERFACE as 目的介面,
    ISNULL(SOURCE_GROUP, 
        CASE 
            WHEN SOURCE_IP_START IS NOT NULL THEN SOURCE_IP_START
            WHEN SOURCE_SUBNET IS NOT NULL THEN SOURCE_SUBNET
            ELSE 'Any'
        END) as 來源,
    ISNULL(DESTINATION_GROUP,
        CASE 
            WHEN DESTINATION_IP_START IS NOT NULL THEN DESTINATION_IP_START
            WHEN DESTINATION_SUBNET1 IS NOT NULL THEN DESTINATION_SUBNET1
            ELSE 'Any'
        END) as 目的,
    ISNULL(SERVICE_GROUP,
        CASE 
            WHEN PORT_S IS NOT NULL THEN PROTOCAL + '/' + CAST(PORT_S AS VARCHAR)
            WHEN PROTOCAL IS NOT NULL THEN PROTOCAL
            ELSE 'Any'
        END) as 服務,
    ACTION as 動作,
    ISNULL(HIT, 0) as 命中次數,
    COMMENT as 備註
FROM FIREWALL_RULE_SET
WHERE DEVICE_NAME = 'FortiGate-FW-01'
ORDER BY CAST(RULENO AS INT);

PRINT '';
PRINT '';

-- ================================================================
-- 11. 命中率最高的規則 / Top Hit Rules
-- ================================================================
PRINT '-- 11. 命中率最高的規則 (前10筆) / Top Hit Rules (Top 10)';
PRINT '========================================';

SELECT TOP 10
    DEVICE_NAME as 防火牆,
    RULENO as 規則編號,
    ISNULL(SOURCE_GROUP, 'Individual') as 來源,
    ISNULL(DESTINATION_GROUP, 'Individual') as 目的,
    ISNULL(SERVICE_GROUP, 'Individual') as 服務,
    ACTION as 動作,
    HIT as 命中次數,
    LAST_HIT_DATE as 最後命中時間,
    COMMENT as 備註
FROM FIREWALL_RULE_SET
WHERE HIT IS NOT NULL
ORDER BY HIT DESC;

PRINT '';
PRINT '';

-- ================================================================
-- 12. 使用特定IP群組的規則 / Rules Using Specific IP Groups
-- ================================================================
PRINT '-- 12. 使用 Internal-Network 群組的規則 / Rules Using Internal-Network Group';
PRINT '========================================';

SELECT 
    DEVICE_NAME as 防火牆,
    RULENO as 規則編號,
    CASE 
        WHEN SOURCE_GROUP = 'Internal-Network' THEN '來源'
        WHEN DESTINATION_GROUP = 'Internal-Network' THEN '目的'
        ELSE '未知'
    END as 使用位置,
    ACTION as 動作,
    COMMENT as 備註
FROM FIREWALL_RULE_SET
WHERE SOURCE_GROUP = 'Internal-Network' 
   OR DESTINATION_GROUP = 'Internal-Network'
ORDER BY DEVICE_NAME, CAST(RULENO AS INT);

PRINT '';
PRINT '';

-- ================================================================
-- 13. 使用特定服務群組的規則 / Rules Using Specific Service Groups
-- ================================================================
PRINT '-- 13. 使用 Web-Services 群組的規則 / Rules Using Web-Services Group';
PRINT '========================================';

SELECT 
    DEVICE_NAME as 防火牆,
    RULENO as 規則編號,
    ISNULL(SOURCE_GROUP, 'Any') as 來源,
    ISNULL(DESTINATION_GROUP, 'Any') as 目的,
    ACTION as 動作,
    ISNULL(HIT, 0) as 命中次數,
    COMMENT as 備註
FROM FIREWALL_RULE_SET
WHERE SERVICE_GROUP = 'Web-Services'
ORDER BY DEVICE_NAME, CAST(RULENO AS INT);

PRINT '';
PRINT '';

-- ================================================================
-- 14. 特定防火牆的API設定 / API Configuration for Specific Firewall
-- ================================================================
PRINT '-- 14. CheckPoint 防火牆的API設定 / CheckPoint Firewall API Configuration';
PRINT '========================================';

SELECT 
    BRAND as 廠牌,
    MODEL as 型號,
    FW_VERSION as 版本,
    API_NAME as API名稱,
    API_URL as API網址
FROM FW_API_INFO
WHERE BRAND = 'CheckPoint'
ORDER BY MODEL, FW_VERSION, SEQUENCE;

PRINT '';
PRINT '';

-- ================================================================
-- 15. 資料完整性檢查 / Data Integrity Check
-- ================================================================
PRINT '-- 15. 資料完整性檢查 / Data Integrity Check';
PRINT '========================================';

-- 檢查是否有IP群組明細沒有對應的主表
SELECT 
    'IP群組明細孤立記錄 / Orphaned IP Group Details' as 檢查項目,
    COUNT(*) as 記錄數
FROM IP_GROUP_DETAILS d
LEFT JOIN IP_GROUP_MAIN m ON d.IP_GROUP_MAIN_ID = m.IP_GROUP_ID
WHERE m.IP_GROUP_ID IS NULL

UNION ALL

-- 檢查是否有服務群組明細沒有對應的主表
SELECT 
    '服務群組明細孤立記錄 / Orphaned Service Group Details' as 檢查項目,
    COUNT(*) as 記錄數
FROM SERVICE_GROUP_DETAILS d
LEFT JOIN SERVICE_GROUP_MAIN m ON d.SERVICE_GROUP_ID = m.SERVICE_GROUP_ID
WHERE m.SERVICE_GROUP_ID IS NULL

UNION ALL

-- 檢查是否有規則引用不存在的IP群組
SELECT 
    '規則引用不存在的來源IP群組 / Rules with Non-existent Source IP Groups' as 檢查項目,
    COUNT(DISTINCT r.SOURCE_GROUP) as 記錄數
FROM FIREWALL_RULE_SET r
LEFT JOIN IP_GROUP_MAIN m ON r.SOURCE_GROUP = m.NAME
WHERE r.SOURCE_GROUP IS NOT NULL 
  AND r.SOURCE_GROUP <> ''
  AND m.IP_GROUP_ID IS NULL

UNION ALL

SELECT 
    '規則引用不存在的目的IP群組 / Rules with Non-existent Dest IP Groups' as 檢查項目,
    COUNT(DISTINCT r.DESTINATION_GROUP) as 記錄數
FROM FIREWALL_RULE_SET r
LEFT JOIN IP_GROUP_MAIN m ON r.DESTINATION_GROUP = m.NAME
WHERE r.DESTINATION_GROUP IS NOT NULL 
  AND r.DESTINATION_GROUP <> ''
  AND m.IP_GROUP_ID IS NULL

UNION ALL

-- 檢查是否有規則引用不存在的服務群組
SELECT 
    '規則引用不存在的服務群組 / Rules with Non-existent Service Groups' as 檢查項目,
    COUNT(DISTINCT r.SERVICE_GROUP) as 記錄數
FROM FIREWALL_RULE_SET r
LEFT JOIN SERVICE_GROUP_MAIN m ON r.SERVICE_GROUP = m.NAME
WHERE r.SERVICE_GROUP IS NOT NULL 
  AND r.SERVICE_GROUP <> ''
  AND m.SERVICE_GROUP_ID IS NULL;

PRINT '';
PRINT '';

PRINT '========================================';
PRINT '查詢完成！/ Query Completed!';
PRINT '========================================';
GO
