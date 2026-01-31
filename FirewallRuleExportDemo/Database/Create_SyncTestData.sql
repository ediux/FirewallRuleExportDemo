-- Created by GitHub Copilot in SSMS - review carefully before executing

-- ============================================
-- 測試資料準備腳本
-- ============================================

-- 備份現有資料 (可選)
SELECT * INTO dbo.FIREWALL_RULE_SET_BACKUP FROM dbo.FIREWALL_RULE_SET;
SELECT * INTO dbo.FW_RULE_USE_BACKUP FROM dbo.FW_RULE_USE;
SELECT * INTO dbo.FW_RULE_SET_BACKUP FROM dbo.FW_RULE_SET;
SELECT * INTO dbo.FW_RULE_RELATION_BACKUP FROM dbo.FW_RULE_RELATION;

-- 插入測試資料到 FIREWALL_RULE_SET
INSERT INTO dbo.FIREWALL_RULE_SET (
    DEVICE_NAME, RULENO, UUID, SAMEKEY,
    SOURCE_IP_START, DESTINATION_IP_START,
    PROTOCAL, PORT_S, PORT_E, ACTION, ENABLED
)
VALUES 
    -- 測試案例1: 完全匹配
    ('FW-TEST-01', 'RULE001', 'UUID-001', 1, '192.168.1.10', '10.0.0.5', 'TCP', 80, 80, 'ALLOW', 1),
    -- 測試案例2: 通訊埠範圍匹配
    ('FW-TEST-01', 'RULE002', 'UUID-002', 2, '192.168.1.20', '10.0.0.10', 'TCP', 8000, 9000, 'ALLOW', 1),
    -- 測試案例3: 無匹配規則
    ('FW-TEST-01', 'RULE003', 'UUID-003', 3, '172.16.0.1', '172.16.0.2', 'UDP', 53, 53, 'DENY', 1),
    -- 測試案例4: 停用的規則
    ('FW-TEST-01', 'RULE004', 'UUID-004', 4, '192.168.2.10', '10.0.0.20', 'TCP', 443, 443, 'ALLOW', 0);

-- 插入測試資料到 FW_RULE_USE
INSERT INTO dbo.FW_RULE_USE (
    DEVICE_NAME, APPLY_EMPID, SRC_IP, DEST_IP,
    PROTOCAL, PORT_S, PORT_E, ENABLED, APPCODE, OWNER_EMPID
)
VALUES
    -- 對應 RULE001 - 完全匹配
    ('FW-TEST-01', 'EMP001', '192.168.1.10', '10.0.0.5', 'TCP', 80, 80, 1, 'APP001', 'OWNER001'),
    -- 對應 RULE002 - 通訊埠範圍內
    ('FW-TEST-01', 'EMP002', '192.168.1.20', '10.0.0.10', 'TCP', 8080, 8080, 1, 'APP002', 'OWNER002'),
    -- 額外的使用規則 - 無對應的 RULE_SET
    ('FW-TEST-01', 'EMP003', '192.168.99.99', '10.0.99.99', 'TCP', 22, 22, 1, 'APP003', 'OWNER003'),
    -- 停用的使用規則
    ('FW-TEST-01', 'EMP004', '192.168.1.10', '10.0.0.5', 'TCP', 80, 80, 0, 'APP004', 'OWNER004');

GO