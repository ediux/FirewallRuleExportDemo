-- 防火牆規則匯出系統 - 測試資料腳本
-- Firewall Rule Export System - Test Data Script
-- ================================================================

USE CMDB;
GO

PRINT '開始插入測試資料...';
PRINT 'Starting to insert test data...';
PRINT '';

-- ================================================================
-- 1. 清空現有測試資料 / Clear existing test data
-- ================================================================
PRINT '清空現有測試資料 / Clearing existing test data...';

DELETE FROM FIREWALL_RULE_SET;
DELETE FROM IP_GROUP_DETAILS;
DELETE FROM IP_GROUP_MAIN;
DELETE FROM SERVICE_GROUP_DETAILS;
DELETE FROM SERVICE_GROUP_MAIN;
DELETE FROM FW_API_INFO;
DELETE FROM FW_HW_INFO;

PRINT '清空完成 / Clear completed.';
PRINT '';

-- ================================================================
-- 2. 防火牆硬體資訊測試資料 / Firewall Hardware Info Test Data
-- ================================================================
PRINT '插入防火牆硬體資訊 / Inserting firewall hardware information...';

-- CheckPoint 防火牆
INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
VALUES 
    ('CheckPoint', 'R80.40', 'R80.40', 'CheckPoint-FW-01', '192.168.100.1', 'CheckPoint 總部主要防火牆'),
    ('CheckPoint', 'R80.40', 'R80.40', 'CheckPoint-FW-02', '192.168.100.11', 'CheckPoint 分公司防火牆'),
    ('CheckPoint', 'R81.10', 'R81.10', 'CheckPoint-FW-03', '192.168.100.21', 'CheckPoint DMZ區域防火牆');

-- FortiGate 防火牆
INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
VALUES 
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'FortiGate-FW-01', '192.168.100.2', 'FortiGate 資料中心主要防火牆'),
    ('FortiGate', 'FortiGate-400E', 'v6.4.8', 'FortiGate-FW-02', '192.168.100.12', 'FortiGate 辦公室防火牆'),
    ('FortiGate', 'FortiGate-200F', 'v7.0.0', 'FortiGate-FW-03', '192.168.100.22', 'FortiGate 遠端站點防火牆');

PRINT '  ✓ 已插入 6 筆防火牆硬體資訊';
PRINT '  ✓ Inserted 6 firewall hardware records';
PRINT '';

-- ================================================================
-- 3. 防火牆 API 資訊測試資料 / Firewall API Info Test Data
-- ================================================================
PRINT '插入防火牆 API 資訊 / Inserting firewall API information...';

-- CheckPoint R80.40 API 資訊
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('CheckPoint', 'R80.40', 'R80.40', 'Login_Session', 'https://192.168.100.1/web_api/login'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Logout_Session', 'https://192.168.100.1/web_api/logout'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallRules', 'https://192.168.100.1/web_api/show-access-rulebase'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_IPGroupInformation', 'https://192.168.100.1/web_api/show-group'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_ServiceGroupInformation', 'https://192.168.100.1/web_api/show-service-group'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallPolicyHits', 'https://192.168.100.1/web_api/show-access-rulebase-hits'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallAddressObjects', 'https://192.168.100.1/web_api/show-objects'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Update_Session', 'https://192.168.100.1/web_api/keepalive');

-- CheckPoint R81.10 API 資訊
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('CheckPoint', 'R81.10', 'R81.10', 'Login_Session', 'https://192.168.100.21/web_api/v1.9/login'),
    ('CheckPoint', 'R81.10', 'R81.10', 'Logout_Session', 'https://192.168.100.21/web_api/v1.9/logout'),
    ('CheckPoint', 'R81.10', 'R81.10', 'Get_FirewallRules', 'https://192.168.100.21/web_api/v1.9/show-access-rulebase'),
    ('CheckPoint', 'R81.10', 'R81.10', 'Get_IPGroupInformation', 'https://192.168.100.21/web_api/v1.9/show-group'),
    ('CheckPoint', 'R81.10', 'R81.10', 'Get_ServiceGroupInformation', 'https://192.168.100.21/web_api/v1.9/show-service-group');

-- FortiGate v7.0.0 API 資訊
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallRules', 'https://192.168.100.2/api/v2/cmdb/firewall/policy'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_IPGroupInformation', 'https://192.168.100.2/api/v2/cmdb/firewall/addrgrp'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_ServiceGroupInformation', 'https://192.168.100.2/api/v2/cmdb/firewall.service/group'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallAddressObjects', 'https://192.168.100.2/api/v2/cmdb/firewall/address'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallServiceObjects', 'https://192.168.100.2/api/v2/cmdb/firewall.service/custom'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallInterfaces', 'https://192.168.100.2/api/v2/cmdb/system/interface'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallSystemInfo', 'https://192.168.100.2/api/v2/monitor/system/status');

-- FortiGate v6.4.8 API 資訊
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('FortiGate', 'FortiGate-400E', 'v6.4.8', 'Get_FirewallRules', 'https://192.168.100.12/api/v2/cmdb/firewall/policy'),
    ('FortiGate', 'FortiGate-400E', 'v6.4.8', 'Get_IPGroupInformation', 'https://192.168.100.12/api/v2/cmdb/firewall/addrgrp'),
    ('FortiGate', 'FortiGate-400E', 'v6.4.8', 'Get_ServiceGroupInformation', 'https://192.168.100.12/api/v2/cmdb/firewall.service/group');

PRINT '  ✓ 已插入 28 筆防火牆 API 資訊';
PRINT '  ✓ Inserted 28 firewall API records';
PRINT '';

-- ================================================================
-- 4. IP 群組主表測試資料 / IP Group Main Test Data
-- ================================================================
PRINT '插入 IP 群組資訊 / Inserting IP group information...';

-- 插入 IP 群組主表資料
INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Internal-Network');
DECLARE @IPGroup_Internal INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Web-Servers');
DECLARE @IPGroup_WebServers INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Database-Servers');
DECLARE @IPGroup_DBServers INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('DMZ-Network');
DECLARE @IPGroup_DMZ INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Management-Network');
DECLARE @IPGroup_Management INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('External-Partners');
DECLARE @IPGroup_Partners INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('VPN-Users');
DECLARE @IPGroup_VPN INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Mail-Servers');
DECLARE @IPGroup_Mail INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('DNS-Servers');
DECLARE @IPGroup_DNS INT = SCOPE_IDENTITY();

INSERT INTO IP_GROUP_MAIN (NAME) VALUES ('Branch-Office');
DECLARE @IPGroup_Branch INT = SCOPE_IDENTITY();

-- ================================================================
-- 5. IP 群組明細測試資料 / IP Group Details Test Data
-- ================================================================

-- Internal-Network 群組明細 (子網路類型)
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_Internal, 3, NULL, NULL, '192.168.1.0/24'),
    (@IPGroup_Internal, 3, NULL, NULL, '192.168.2.0/24'),
    (@IPGroup_Internal, 3, NULL, NULL, '192.168.10.0/23');

-- Web-Servers 群組明細 (IP範圍類型)
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_WebServers, 2, '10.0.0.10', '10.0.0.20', NULL),
    (@IPGroup_WebServers, 1, '10.0.0.100', '10.0.0.100', NULL);

-- Database-Servers 群組明細 (單一IP類型)
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_DBServers, 1, '10.10.10.10', '10.10.10.10', NULL),
    (@IPGroup_DBServers, 1, '10.10.10.11', '10.10.10.11', NULL),
    (@IPGroup_DBServers, 1, '10.10.10.12', '10.10.10.12', NULL),
    (@IPGroup_DBServers, 2, '10.10.10.20', '10.10.10.30', NULL);

-- DMZ-Network 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_DMZ, 3, NULL, NULL, '172.16.0.0/24'),
    (@IPGroup_DMZ, 3, NULL, NULL, '172.16.1.0/24');

-- Management-Network 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_Management, 3, NULL, NULL, '10.255.255.0/24');

-- External-Partners 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_Partners, 1, '203.0.113.10', '203.0.113.10', NULL),
    (@IPGroup_Partners, 1, '203.0.113.20', '203.0.113.20', NULL),
    (@IPGroup_Partners, 2, '198.51.100.1', '198.51.100.50', NULL);

-- VPN-Users 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_VPN, 3, NULL, NULL, '10.8.0.0/24');

-- Mail-Servers 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_Mail, 1, '10.20.20.10', '10.20.20.10', NULL),
    (@IPGroup_Mail, 1, '10.20.20.11', '10.20.20.11', NULL);

-- DNS-Servers 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_DNS, 1, '10.30.30.10', '10.30.30.10', NULL),
    (@IPGroup_DNS, 1, '10.30.30.11', '10.30.30.11', NULL),
    (@IPGroup_DNS, 1, '8.8.8.8', '8.8.8.8', NULL),
    (@IPGroup_DNS, 1, '8.8.4.4', '8.8.4.4', NULL);

-- Branch-Office 群組明細
INSERT INTO IP_GROUP_DETAILS (IP_GROUP_MAIN_ID, IPTYPE, IPSTART, IPEND, IPSUBNET)
VALUES 
    (@IPGroup_Branch, 3, NULL, NULL, '192.168.100.0/24'),
    (@IPGroup_Branch, 3, NULL, NULL, '192.168.200.0/24');

PRINT '  ✓ 已插入 10 個 IP 群組及 33 筆明細資料';
PRINT '  ✓ Inserted 10 IP groups with 33 detail records';
PRINT '';

-- ================================================================
-- 6. 服務群組主表測試資料 / Service Group Main Test Data
-- ================================================================
PRINT '插入服務群組資訊 / Inserting service group information...';

-- 插入服務群組主表資料
INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('HTTP-Services');
DECLARE @SvcGroup_HTTP INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('HTTPS-Services');
DECLARE @SvcGroup_HTTPS INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('Mail-Services');
DECLARE @SvcGroup_Mail INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('Database-Services');
DECLARE @SvcGroup_Database INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('Remote-Access');
DECLARE @SvcGroup_Remote INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('DNS-Services');
DECLARE @SvcGroup_DNS INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('FTP-Services');
DECLARE @SvcGroup_FTP INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('Web-Services');
DECLARE @SvcGroup_Web INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('Management-Services');
DECLARE @SvcGroup_Mgmt INT = SCOPE_IDENTITY();

INSERT INTO SERVICE_GROUP_MAIN (NAME) VALUES ('VPN-Services');
DECLARE @SvcGroup_VPN INT = SCOPE_IDENTITY();

-- ================================================================
-- 7. 服務群組明細測試資料 / Service Group Details Test Data
-- ================================================================

-- HTTP-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_HTTP, 'TCP', '80', '80'),
    (@SvcGroup_HTTP, 'TCP', '8080', '8080'),
    (@SvcGroup_HTTP, 'TCP', '8000', '8000'),
    (@SvcGroup_HTTP, 'TCP', '8888', '8888');

-- HTTPS-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_HTTPS, 'TCP', '443', '443'),
    (@SvcGroup_HTTPS, 'TCP', '8443', '8443');

-- Mail-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_Mail, 'TCP', '25', '25'),
    (@SvcGroup_Mail, 'TCP', '110', '110'),
    (@SvcGroup_Mail, 'TCP', '143', '143'),
    (@SvcGroup_Mail, 'TCP', '465', '465'),
    (@SvcGroup_Mail, 'TCP', '587', '587'),
    (@SvcGroup_Mail, 'TCP', '993', '993'),
    (@SvcGroup_Mail, 'TCP', '995', '995');

-- Database-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_Database, 'TCP', '1433', '1433'),
    (@SvcGroup_Database, 'TCP', '3306', '3306'),
    (@SvcGroup_Database, 'TCP', '5432', '5432'),
    (@SvcGroup_Database, 'TCP', '1521', '1521'),
    (@SvcGroup_Database, 'TCP', '27017', '27017');

-- Remote-Access 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_Remote, 'TCP', '22', '22'),
    (@SvcGroup_Remote, 'TCP', '23', '23'),
    (@SvcGroup_Remote, 'TCP', '3389', '3389'),
    (@SvcGroup_Remote, 'TCP', '5900', '5900');

-- DNS-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_DNS, 'TCP', '53', '53'),
    (@SvcGroup_DNS, 'UDP', '53', '53');

-- FTP-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_FTP, 'TCP', '20', '21'),
    (@SvcGroup_FTP, 'TCP', '21', '21'),
    (@SvcGroup_FTP, 'TCP', '989', '990');

-- Web-Services 群組明細 (包含 HTTP 和 HTTPS)
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_Web, 'TCP', '80', '80'),
    (@SvcGroup_Web, 'TCP', '443', '443'),
    (@SvcGroup_Web, 'TCP', '8080', '8080'),
    (@SvcGroup_Web, 'TCP', '8443', '8443');

-- Management-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_Mgmt, 'TCP', '161', '162'),
    (@SvcGroup_Mgmt, 'UDP', '161', '162'),
    (@SvcGroup_Mgmt, 'TCP', '22', '22'),
    (@SvcGroup_Mgmt, 'TCP', '3389', '3389');

-- VPN-Services 群組明細
INSERT INTO SERVICE_GROUP_DETAILS (SERVICE_GROUP_ID, PROTOCOL, PORT_S, PORT_E)
VALUES 
    (@SvcGroup_VPN, 'UDP', '500', '500'),
    (@SvcGroup_VPN, 'UDP', '4500', '4500'),
    (@SvcGroup_VPN, 'TCP', '1723', '1723'),
    (@SvcGroup_VPN, 'UDP', '1194', '1194');

PRINT '  ✓ 已插入 10 個服務群組及 43 筆明細資料';
PRINT '  ✓ Inserted 10 service groups with 43 detail records';
PRINT '';

-- ================================================================
-- 8. 範例防火牆規則測試資料 / Sample Firewall Rules Test Data
-- ================================================================
PRINT '插入範例防火牆規則 / Inserting sample firewall rules...';

-- CheckPoint-FW-01 範例規則
INSERT INTO FIREWALL_RULE_SET 
(DEVICE_NAME, RULENO, UUID, SAMEKEY, INCOMING_INTERFACE, OUTGOING_INTERFACE,
 SOURCE_GROUP, SOURCE_IP_START, SOURCE_IP_END, SOURCE_FQDN, SOURCE_SUBNET,
 DESTINATION_GROUP, DESTINATION_IP_START, DESTINATION_IP_END, DESTINATION_FQDN, DESTINATION_SUBNET1,
 PROTOCAL, PORT_S, PORT_E, SERVICE_GROUP, ACTION, ENABLED, DATA_DT, HIT,
 FIRST_HIT_DATE, LAST_HIT_DATE, LayerObject, VDOM, HAS_EXPIRE_DATE, EXPIRE_DATE, COMMENT)
VALUES 
    ('CheckPoint-FW-01', '1', 'rule-cp-001', 1, 'eth0', 'eth1',
     'Internal-Network', NULL, NULL, NULL, NULL,
     'Web-Servers', NULL, NULL, NULL, NULL,
     'TCP', 80, 80, NULL, 'Accept', 1, '2024-01-31', 1500,
     '2024-01-01 08:00:00', '2024-01-31 17:30:00', 'Network', NULL, 0, NULL, '允許內網存取Web伺服器'),
     
    ('CheckPoint-FW-01', '2', 'rule-cp-002', 2, 'eth0', 'eth1',
     'Internal-Network', NULL, NULL, NULL, NULL,
     'Database-Servers', NULL, NULL, NULL, NULL,
     'TCP', 1433, 1433, NULL, 'Accept', 1, '2024-01-31', 850,
     '2024-01-01 08:00:00', '2024-01-31 16:45:00', 'Network', NULL, 0, NULL, '允許內網存取資料庫'),
     
    ('CheckPoint-FW-01', '3', 'rule-cp-003', 3, 'eth2', 'eth1',
     NULL, '203.0.113.10', '203.0.113.10', NULL, NULL,
     'DMZ-Network', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'HTTPS-Services', 'Accept', 1, '2024-01-31', 320,
     '2024-01-15 09:00:00', '2024-01-31 14:20:00', 'Network', NULL, 0, NULL, '允許外部合作夥伴存取DMZ'),
     
    ('CheckPoint-FW-01', '4', 'rule-cp-004', 4, 'any', 'any',
     'VPN-Users', NULL, NULL, NULL, NULL,
     'Internal-Network', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'Remote-Access', 'Accept', 1, '2024-01-31', 2100,
     '2024-01-01 08:00:00', '2024-01-31 18:00:00', 'Network', NULL, 0, NULL, 'VPN使用者遠端存取'),
     
    ('CheckPoint-FW-01', '5', 'rule-cp-005', 5, 'eth1', 'eth2',
     'Mail-Servers', NULL, NULL, NULL, NULL,
     NULL, '0.0.0.0', '255.255.255.255', NULL, NULL,
     NULL, NULL, NULL, 'Mail-Services', 'Accept', 1, '2024-01-31', 450,
     '2024-01-01 08:00:00', '2024-01-31 17:00:00', 'Network', NULL, 0, NULL, '郵件伺服器對外連線');

-- FortiGate-FW-01 範例規則
INSERT INTO FIREWALL_RULE_SET 
(DEVICE_NAME, RULENO, UUID, SAMEKEY, INCOMING_INTERFACE, OUTGOING_INTERFACE,
 SOURCE_GROUP, SOURCE_IP_START, SOURCE_IP_END, SOURCE_FQDN, SOURCE_SUBNET,
 DESTINATION_GROUP, DESTINATION_IP_START, DESTINATION_IP_END, DESTINATION_FQDN, DESTINATION_SUBNET1,
 PROTOCAL, PORT_S, PORT_E, SERVICE_GROUP, ACTION, ENABLED, DATA_DT, HIT,
 FIRST_HIT_DATE, LAST_HIT_DATE, LayerObject, VDOM, HAS_EXPIRE_DATE, EXPIRE_DATE, COMMENT)
VALUES 
    ('FortiGate-FW-01', '1', 'rule-fg-001', 1, 'port1', 'port2',
     'Internal-Network', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'Web-Services', 'accept', 1, '2024-01-31', 3200,
     '2024-01-01 00:00:00', '2024-01-31 23:59:59', NULL, 'root', 0, NULL, '內網存取Web服務'),
     
    ('FortiGate-FW-01', '2', 'rule-fg-002', 2, 'port1', 'port3',
     'Branch-Office', NULL, NULL, NULL, NULL,
     'Database-Servers', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'Database-Services', 'accept', 1, '2024-01-31', 1800,
     '2024-01-01 00:00:00', '2024-01-31 20:00:00', NULL, 'root', 0, NULL, '分公司存取資料庫'),
     
    ('FortiGate-FW-01', '3', 'rule-fg-003', 3, 'port4', 'port1',
     NULL, NULL, NULL, NULL, NULL,
     'DNS-Servers', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'DNS-Services', 'accept', 1, '2024-01-31', 5600,
     '2024-01-01 00:00:00', '2024-01-31 23:59:00', NULL, 'root', 0, NULL, 'DNS查詢服務'),
     
    ('FortiGate-FW-01', '4', 'rule-fg-004', 4, 'port2', 'port1',
     'Web-Servers', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, NULL, NULL,
     'TCP', 3306, 3306, NULL, 'accept', 1, '2024-01-31', 950,
     '2024-01-01 00:00:00', '2024-01-31 18:30:00', NULL, 'root', 0, NULL, 'Web伺服器存取MySQL'),
     
    ('FortiGate-FW-01', '5', 'rule-fg-005', 5, 'any', 'any',
     'Management-Network', NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, NULL, NULL,
     NULL, NULL, NULL, 'Management-Services', 'accept', 1, '2024-01-31', 680,
     '2024-01-01 00:00:00', '2024-01-31 19:00:00', NULL, 'root', 0, NULL, '管理網路存取'),
     
    ('FortiGate-FW-01', '6', 'rule-fg-006', 6, 'port5', 'port2',
     'External-Partners', NULL, NULL, NULL, NULL,
     'Web-Servers', NULL, NULL, NULL, NULL,
     'TCP', 443, 443, NULL, 'accept', 1, '2024-01-31', 1250,
     '2024-01-10 00:00:00', '2024-01-31 22:00:00', NULL, 'root', 1, '2024-12-31', '合作夥伴HTTPS存取');

PRINT '  ✓ 已插入 11 筆範例防火牆規則';
PRINT '  ✓ Inserted 11 sample firewall rules';
PRINT '';

-- ================================================================
-- 9. 查詢驗證 / Query Verification
-- ================================================================
PRINT '========================================';
PRINT '資料插入完成統計 / Data Insertion Summary';
PRINT '========================================';
PRINT '';

DECLARE @FWCount INT, @APICount INT, @IPGroupCount INT, @IPDetailCount INT;
DECLARE @SvcGroupCount INT, @SvcDetailCount INT, @RuleCount INT;

SELECT @FWCount = COUNT(*) FROM FW_HW_INFO;
SELECT @APICount = COUNT(*) FROM FW_API_INFO;
SELECT @IPGroupCount = COUNT(*) FROM IP_GROUP_MAIN;
SELECT @IPDetailCount = COUNT(*) FROM IP_GROUP_DETAILS;
SELECT @SvcGroupCount = COUNT(*) FROM SERVICE_GROUP_MAIN;
SELECT @SvcDetailCount = COUNT(*) FROM SERVICE_GROUP_DETAILS;
SELECT @RuleCount = COUNT(*) FROM FIREWALL_RULE_SET;

PRINT '防火牆硬體資訊 / Firewall Hardware Info: ' + CAST(@FWCount AS VARCHAR(10)) + ' 筆';
PRINT '防火牆API資訊 / Firewall API Info: ' + CAST(@APICount AS VARCHAR(10)) + ' 筆';
PRINT 'IP群組 / IP Groups: ' + CAST(@IPGroupCount AS VARCHAR(10)) + ' 個';
PRINT 'IP群組明細 / IP Group Details: ' + CAST(@IPDetailCount AS VARCHAR(10)) + ' 筆';
PRINT '服務群組 / Service Groups: ' + CAST(@SvcGroupCount AS VARCHAR(10)) + ' 個';
PRINT '服務群組明細 / Service Group Details: ' + CAST(@SvcDetailCount AS VARCHAR(10)) + ' 筆';
PRINT '防火牆規則 / Firewall Rules: ' + CAST(@RuleCount AS VARCHAR(10)) + ' 筆';
PRINT '';

-- ================================================================
-- 10. 查詢範例 / Query Examples
-- ================================================================
PRINT '========================================';
PRINT '查詢範例 / Query Examples';
PRINT '========================================';
PRINT '';

PRINT '-- 查詢所有防火牆裝置 / Query all firewall devices';
PRINT 'SELECT * FROM FW_HW_INFO;';
PRINT '';

PRINT '-- 查詢特定防火牆的規則 / Query rules for specific firewall';
PRINT 'SELECT * FROM FIREWALL_RULE_SET WHERE DEVICE_NAME = ''CheckPoint-FW-01'';';
PRINT '';

PRINT '-- 查詢IP群組資訊 / Query IP group information';
PRINT 'SELECT m.NAME, d.IPTYPE, d.IPSTART, d.IPEND, d.IPSUBNET';
PRINT 'FROM IP_GROUP_MAIN m';
PRINT 'INNER JOIN IP_GROUP_DETAILS d ON m.IP_GROUP_ID = d.IP_GROUP_MAIN_ID';
PRINT 'WHERE m.NAME = ''Internal-Network'';';
PRINT '';

PRINT '-- 查詢服務群組資訊 / Query service group information';
PRINT 'SELECT m.NAME, d.PROTOCOL, d.PORT_S, d.PORT_E';
PRINT 'FROM SERVICE_GROUP_MAIN m';
PRINT 'INNER JOIN SERVICE_GROUP_DETAILS d ON m.SERVICE_GROUP_ID = d.SERVICE_GROUP_ID';
PRINT 'WHERE m.NAME = ''Web-Services'';';
PRINT '';

PRINT '-- 統計各防火牆規則數量 / Count rules by firewall';
PRINT 'SELECT DEVICE_NAME, COUNT(*) as RuleCount';
PRINT 'FROM FIREWALL_RULE_SET';
PRINT 'GROUP BY DEVICE_NAME;';
PRINT '';

PRINT '========================================';
PRINT '測試資料插入完成！/ Test data insertion completed!';
PRINT '========================================';
GO
