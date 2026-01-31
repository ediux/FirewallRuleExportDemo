-- Created by GitHub Copilot in SSMS - review carefully before executing

-- ============================================
-- 測試結果驗證腳本
-- ============================================

PRINT '========================================';
PRINT '測試案例 1: 檢查資料同步是否正確';
PRINT '========================================';

SELECT 
    '來源資料筆數' AS 檢查項目,
    COUNT(*) AS 筆數
FROM dbo.FIREWALL_RULE_SET
UNION ALL
SELECT 
    '目標資料筆數',
    COUNT(*)
FROM dbo.FW_RULE_SET;

PRINT '';
PRINT '========================================';
PRINT '測試案例 2: 檢查 DIFF 欄位設定';
PRINT '========================================';

SELECT 
    DIFF AS 狀態,
    CASE DIFF
        WHEN '1' THEN '有匹配規則'
        WHEN '2' THEN '無匹配規則'
        ELSE '未設定'
    END AS 說明,
    COUNT(*) AS 筆數
FROM dbo.FW_RULE_SET
GROUP BY DIFF
ORDER BY DIFF;

PRINT '';
PRINT '========================================';
PRINT '測試案例 3: 檢查關聯資料表';
PRINT '========================================';

SELECT 
    rel.FW_RULE_SET_NO,
    rel.FW_RULE_USE_NO,
    rs.RULENO,
    rs.SRC_IP AS 規則集來源IP,
    ru.SRC_IP AS 使用規則來源IP,
    rs.DEST_IP AS 規則集目的IP,
    ru.DEST_IP AS 使用規則目的IP,
    rs.PROTOCAL AS 規則集協定,
    ru.PROTOCAL AS 使用規則協定,
    rs.PORT_S AS 規則集起始埠,
    rs.PORT_E AS 規則集結束埠,
    ru.PORT_S AS 使用規則起始埠,
    ru.PORT_E AS 使用規則結束埠,
    rel.MEMO AS 匹配備註
FROM dbo.FW_RULE_RELATION rel
INNER JOIN dbo.FW_RULE_SET rs ON rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
INNER JOIN dbo.FW_RULE_USE ru ON rel.FW_RULE_USE_NO = ru.FW_RULE_USE_NO;

PRINT '';
PRINT '========================================';
PRINT '測試案例 4: 詳細規則比對結果';
PRINT '========================================';

SELECT 
    rs.FW_RULE_SET_NO,
    rs.RULENO,
    rs.UUID,
    rs.SRC_IP,
    rs.DEST_IP,
    rs.PROTOCAL,
    rs.PORT_S,
    rs.PORT_E,
    rs.DIFF,
    CASE rs.DIFF
        WHEN '1' THEN '已匹配'
        WHEN '2' THEN '未匹配'
        ELSE '狀態異常'
    END AS 匹配狀態,
    (
        SELECT COUNT(*) 
        FROM dbo.FW_RULE_RELATION rel 
        WHERE rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
    ) AS 匹配數量
FROM dbo.FW_RULE_SET rs
ORDER BY rs.FW_RULE_SET_NO;

PRINT '';
PRINT '========================================';
PRINT '測試案例 5: 檢查異常狀況';
PRINT '========================================';

-- 檢查是否有 DIFF 未正確設定的記錄
SELECT 
    '未正確設定 DIFF' AS 異常類型,
    COUNT(*) AS 異常筆數
FROM dbo.FW_RULE_SET
WHERE DIFF NOT IN ('1', '2')
UNION ALL
-- 檢查是否有關聯記錄但 DIFF 不是 1
SELECT 
    '有關聯但 DIFF 不為 1',
    COUNT(*)
FROM dbo.FW_RULE_SET rs
WHERE EXISTS (
    SELECT 1 FROM dbo.FW_RULE_RELATION rel 
    WHERE rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
) AND rs.DIFF != '1'
UNION ALL
-- 檢查是否有 DIFF=1 但無關聯記錄
SELECT 
    'DIFF 為 1 但無關聯記錄',
    COUNT(*)
FROM dbo.FW_RULE_SET rs
WHERE rs.DIFF = '1' AND NOT EXISTS (
    SELECT 1 FROM dbo.FW_RULE_RELATION rel 
    WHERE rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
);

PRINT '';
PRINT '========================================';
PRINT '測試案例 6: 通訊埠範圍匹配驗證';
PRINT '========================================';

SELECT 
    rs.RULENO,
    rs.PORT_S AS 規則起始埠,
    rs.PORT_E AS 規則結束埠,
    ru.PORT_S AS 使用起始埠,
    ru.PORT_E AS 使用結束埠,
    CASE 
        WHEN ru.PORT_S BETWEEN rs.PORT_S AND rs.PORT_E THEN '✓ 起始埠在範圍內'
        WHEN ru.PORT_E BETWEEN rs.PORT_S AND rs.PORT_E THEN '✓ 結束埠在範圍內'
        WHEN rs.PORT_S BETWEEN ru.PORT_S AND ru.PORT_E THEN '✓ 規則範圍被包含'
        ELSE '✗ 不在範圍內'
    END AS 匹配邏輯,
    rel.MEMO
FROM dbo.FW_RULE_RELATION rel
INNER JOIN dbo.FW_RULE_SET rs ON rel.FW_RULE_SET_NO = rs.FW_RULE_SET_NO
INNER JOIN dbo.FW_RULE_USE ru ON rel.FW_RULE_USE_NO = ru.FW_RULE_USE_NO;

GO