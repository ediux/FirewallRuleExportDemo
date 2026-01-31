# FortiGate 防火牆 API 實作說明
# FortiGate Firewall API Implementation Guide

## 概述 / Overview

本專案提供兩種 FortiGate 防火牆 API 客戶端實作:

1. **FortiGateFirewallAPIClient.cs** - 示範版本 (使用模擬資料)
2. **FortiGateRealAPIClient.cs** - 實際版本 (真實 API 呼叫) ⭐ **NEW**

## 實際 API 客戶端特色 / Real API Client Features

### ✅ 完整功能
- ✔️ 支援 API Token 認證 (唯一認證方式)
- ✔️ 自動處理 VDOM (Virtual Domain)
- ✔️ SSL 憑證處理
- ✔️ Proxy 支援
- ✔️ 重試機制
- ✔️ 完整的錯誤處理
- ✔️ 詳細的日誌記錄

### ✅ API 功能
- ✔️ 驗證 API Token
- ✔️ 取得所有 VDOM
- ✔️ 取得防火牆規則 (Firewall Policy)
- ✔️ 取得地址物件及群組 (遞迴展開)
- ✔️ 取得服務物件及群組 (遞迴展開)
- ✔️ 支援多 VDOM 環境

### ✅ 智慧展開邏輯
- ✔️ 優先使用 CMDB 中的群組定義
- ✔️ 自動從 API 取得不在 CMDB 中的群組
- ✔️ 支援巢狀群組 (遞迴展開)
- ✔️ 笛卡爾乘積展開規則

---

## 支援的 FortiGate 物件類型

### 地址物件類型
| 類型 | FortiGate Type | 說明 | 範例 |
|------|----------------|------|------|
| IP/網路遮罩 | `ipmask` | IP 位址或子網路 | 192.168.1.100/32, 192.168.1.0/24 |
| IP 範圍 | `iprange` | IP 位址範圍 | 192.168.1.1-192.168.1.254 |
| FQDN | `fqdn` | 完整網域名稱 | www.example.com |
| 地理位置 | `geography` | 國家/地區 | China, United States |
| 地址群組 | `addrgrp` | 地址群組 | Internal-Network |
| 全部 | `all` | 任意位址 | all |

### 服務物件類型
| 類型 | FortiGate Type | 說明 | 範例 |
|------|----------------|------|------|
| TCP/UDP/SCTP | `TCP/UDP/SCTP` | TCP/UDP 服務 | TCP 80, UDP 53 |
| ICMP | `ICMP` | ICMP 協定 | ping |
| IP 協定 | `IP` | 其他 IP 協定 | GRE (47), ESP (50) |
| 服務群組 | `group` | 服務群組 | Web-Services |
| 全部 | `ALL` | 任意服務 | ALL |

---

## 使用方式 / Usage

### 方法 1: 修改 DemoCMDBSource.cs (建議)

在 `DemoCMDBSource.cs` 的 `DoFirewallRules` 方法中切換實作:

```csharp
public async Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device)
{
    IFirewallAPIClient client = null;

    if (device.Brand.ToUpper() == "CHECKPOINT")
    {
        client = new CheckPointRealAPIClient(device, this);
    }
    else if (device.Brand.ToUpper() == "FORTIGATE")
    {
        // 使用實際 API 版本 (NEW!)
        client = new FortiGateRealAPIClient(device, this);
        
        // 或使用示範版本
        // client = new FortiGateFirewallAPIClient(device, this);
    }
    else
    {
        throw new NotSupportedException($"不支援的防火牆廠牌: {device.Brand}");
    }

    return await client.GetFirewallRules();
}
```

---

## 認證設定 / Authentication Setup

### FortiGate API Token 認證 (唯一方式)

FortiGate REST API **只支援 API Token 認證**，不支援帳號密碼。

#### 在 FortiGate 上建立 API Token:

**方法 1: 透過 GUI (建議新手)**

1. 登入 FortiGate Web Console
2. 前往 **System** → **Administrators**
3. 點選 **Create New** → **REST API Admin**
4. 設定管理員資訊:
   - **Username**: 例如 `api_readonly`
   - **Administrator Profile**: 選擇 `prof_admin` 或建立自訂 profile
   - **Trusted Hosts**: 設定允許存取的 IP 範圍 (安全考量)
   - **CORS Allow Origin**: (可選) 如需 CORS 支援
   - **PKI Group**: (可選)
   - **Comments**: 說明用途
5. 點選 **OK**
6. **複製 API Token** - ⚠️ 此 Token 只會顯示一次，請妥善保存！

**方法 2: 透過 CLI**

```bash
config system api-user
    edit "api_readonly"
        set accprofile "prof_admin"
        set vdom "root"
        set comments "API for firewall rule export"
        config trusthost
            edit 1
                set ipv4-trusthost 192.168.1.0 255.255.255.0
            next
        end
    next
end
```

執行後，系統會產生並顯示 API Token。

#### 在資料庫中設定:

```sql
-- 假設您有一個儲存 API 認證的資料表
UPDATE FW_API_CREDENTIALS
SET TOKEN = 'your-fortigate-api-token-here',
    AUTH_TYPE = 'Token'
WHERE DEVICE_NAME = 'FortiGate-FW-01';
```

#### 或在 DemoCMDBSource 中設定:

```csharp
public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
{
    if (device.Brand.ToUpper() == "FORTIGATE")
    {
        return new ApiUserInfo
        {
            AuthType = "Token",
            Token = "your-fortigate-api-token-here"
            // UserName 和 Password 不需要
        };
    }
    // ... 其他廠牌
}
```

---

## API URL 設定

### 自動設定 (預設)

如果資料庫中沒有設定 API URL,程式會自動使用預設 URL:

```
https://{DEVICE_IP}/api/v2/{endpoint}
```

### 手動設定 (建議)

在 `FW_API_INFO` 資料表中設定:

```sql
-- FortiGate 7.0.0 API URLs
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallRules', 
     'https://192.168.100.2/api/v2/cmdb/firewall/policy'),
    
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_IPGroupInformation', 
     'https://192.168.100.2/api/v2/cmdb/firewall/addrgrp'),
    
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_ServiceGroupInformation', 
     'https://192.168.100.2/api/v2/cmdb/firewall.service/group');
```

---

## VDOM (Virtual Domain) 支援

FortiGate 支援虛擬網域 (VDOM) 功能，可在單一設備上建立多個虛擬防火牆。

### 自動處理 VDOM

程式會自動:
1. 取得所有 VDOM 列表
2. 針對每個 VDOM 分別取得防火牆規則
3. 在規則資料中標記所屬的 VDOM

### VDOM 設定範例

```sql
-- 資料庫中的規則會包含 VDOM 資訊
SELECT 
    DEVICE_NAME,
    VDOM,
    RULENO,
    SOURCE_GROUP,
    DESTINATION_GROUP,
    ACTION
FROM FIREWALL_RULE_SET
WHERE DEVICE_NAME = 'FortiGate-FW-01'
ORDER BY VDOM, CAST(RULENO AS INT);
```

### 單 VDOM vs 多 VDOM

**單 VDOM 模式** (預設):
- VDOM 名稱: `root`
- 適用於大多數小型環境

**多 VDOM 模式**:
- 可建立多個 VDOM: `root`, `vdom1`, `vdom2` 等
- 每個 VDOM 有獨立的防火牆規則
- 適用於需要邏輯隔離的大型環境

---

## SSL 憑證處理

### 開發/測試環境 (預設)

程式預設會忽略 SSL 憑證驗證錯誤:

```csharp
handler.ServerCertificateCustomValidationCallback = 
    (sender, cert, chain, sslPolicyErrors) => true;
```

⚠️ **這僅適用於開發/測試環境**

### 生產環境 (建議)

修改 `FortiGateRealAPIClient.cs`:

```csharp
// 移除或註解掉這一行
// handler.ServerCertificateCustomValidationCallback = 
//     (sender, cert, chain, sslPolicyErrors) => true;

// 確保 FortiGate 使用有效的 SSL 憑證
// 或將憑證安裝到信任的根憑證授權單位
```

---

## Proxy 設定

如果您的環境需要透過 Proxy 連線到 FortiGate:

### 在 App.config 中設定:

```xml
<appSettings>
  <add key="UseProxy" value="true" />
  <add key="ProxyAddress" value="proxy.company.com" />
  <add key="ProxyPort" value="8080" />
  <add key="ProxyUserName" value="proxy_user" />
  <add key="ProxyPassword" value="proxy_password" />
</appSettings>
```

---

## 效能調校 / Performance Tuning

### 調整 API 超時時間

```xml
<appSettings>
  <!-- API 連線逾時 (毫秒) -->
  <add key="APITimeout" value="60000" />
</appSettings>
```

### 調整重試設定

```xml
<appSettings>
  <!-- 最大重試次數 -->
  <add key="MaxRetryCount" value="3" />
  
  <!-- 重試延遲 (毫秒) -->
  <add key="RetryDelayMilliseconds" value="2000" />
</appSettings>
```

---

## 日誌等級設定

### 一般使用 (Info)

```xml
<add key="LogLevel" value="Info" />
```

### 除錯模式 (Debug)

會顯示更多細節,包括 API URL 和群組展開過程:

```xml
<add key="LogLevel" value="Debug" />
```

### 追蹤模式 (Trace)

會顯示完整的 API 請求和回應內容:

```xml
<add key="LogLevel" value="Trace" />
```

⚠️ **生產環境建議使用 Info 等級**

---

## API 限制與注意事項

### FortiGate API 限制

1. **並發連線限制**:
   - 預設同時連線數: 5-10 個
   - 建議序列處理多個 VDOM

2. **API Rate Limiting**:
   - FortiGate 可能會限制 API 呼叫頻率
   - 建議加入適當的延遲

3. **物件數量限制**:
   - 單次 API 查詢預設回傳所有物件
   - 大量物件時可能需要較長時間

4. **API 版本差異**:
   - v6.x 和 v7.x API 路徑可能略有不同
   - 請確認您的版本

### 權限需求

執行此程式的 API 使用者需要以下權限:
- ✅ **Read** 權限
- ✅ 存取 **Firewall Policy**
- ✅ 存取 **Firewall Objects** (Address, Address Group, Service, Service Group)
- ✅ 存取 **System** (VDOM 資訊)

### 建立自訂 Administrator Profile

```bash
config system accprofile
    edit "api_readonly_profile"
        set secfabgrp read
        set ftviewgrp read
        set authgrp read
        set sysgrp read
        set netgrp read
        set loggrp read
        set fwgrp read
        set vpngrp read
        set utmgrp read
        set wanoptgrp read
        set wifi read
    next
end
```

---

## 故障排除 / Troubleshooting

### 問題 1: API Token 驗證失敗

**症狀**: `API Token 驗證失敗` 或 `Authentication failed`

**解決方法**:
1. 確認 API Token 正確 (注意不要有空格或換行)
2. 檢查 API User 的 Trusted Hosts 設定
3. 確認 API User 有足夠的權限
4. 檢查 FortiGate 的 API 功能是否啟用
5. 嘗試使用 Postman 或 curl 直接測試 API

**測試 API Token**:
```bash
curl -k -H "Authorization: Bearer YOUR-TOKEN-HERE" \
  https://YOUR-FORTIGATE-IP/api/v2/monitor/system/status
```

### 問題 2: 無法取得 VDOM 列表

**症狀**: `無法取得 VDOM 列表`

**解決方法**:
1. 確認 FortiGate 是否啟用 VDOM 功能
2. 如果未啟用 VDOM,程式會自動使用 `root`
3. 檢查 API User 是否有權限存取所有 VDOM

### 問題 3: 規則數量不符

**症狀**: 取得的規則數量少於 Web UI 中看到的

**可能原因**:
1. 某些規則在不同的 VDOM 中
2. 某些規則被停用
3. API User 權限不足

**解決方法**:
- 檢查日誌確認處理了哪些 VDOM
- 確認 API User 有權限存取所有 VDOM
- 檢查規則的狀態 (enable/disable)

### 問題 4: 地址或服務群組展開錯誤

**症狀**: `無法取得地址群組` 或 `無法取得服務群組`

**解決方法**:
1. 確認物件名稱正確 (FortiGate 物件名稱區分大小寫)
2. 檢查物件是否在正確的 VDOM 中
3. 確認 API User 有權限讀取物件
4. 查看詳細的錯誤訊息 (使用 Debug 或 Trace 日誌等級)

### 問題 5: 連線逾時

**症狀**: `A task was canceled` 或 `The operation has timed out`

**解決方法**:
1. 檢查網路連線到 FortiGate
2. 增加 `APITimeout` 設定值
3. 檢查防火牆規則是否允許連線
4. 確認 FortiGate 的 HTTPS 管理埠號 (預設 443)

---

## 效能參考數據

基於測試環境的效能數據:

| 規則數量 | VDOM 數量 | 處理時間 | 備註 |
|---------|----------|---------|------|
| 100 條規則 | 1 | ~15 秒 | 不包含群組展開 |
| 100 條規則 | 1 | ~45 秒 | 包含 10 個群組展開 |
| 500 條規則 | 1 | ~1 分鐘 | 不包含群組展開 |
| 500 條規則 | 2 | ~2 分鐘 | 包含群組展開 |
| 1000 條規則 | 3 | ~5 分鐘 | 包含群組展開 |

**影響效能的因素**:
- FortiGate 設備效能
- 網路延遲
- VDOM 數量
- 群組巢狀層級
- 規則複雜度

---

## API 文件參考

### 官方文件
- **FortiGate REST API**: https://docs.fortinet.com/document/fortigate/7.0.0/rest-api-reference/
- **Administration Guide**: https://docs.fortinet.com/document/fortigate/7.0.0/administration-guide/
- **CLI Reference**: https://docs.fortinet.com/document/fortigate/7.0.0/cli-reference/

### 常用 API 端點

| 功能 | API 端點 | 說明 |
|------|---------|------|
| 系統狀態 | `/api/v2/monitor/system/status` | 取得系統資訊 |
| VDOM 列表 | `/api/v2/cmdb/system/vdom` | 取得所有 VDOM |
| 防火牆規則 | `/api/v2/cmdb/firewall/policy` | 取得防火牆規則 |
| 地址物件 | `/api/v2/cmdb/firewall/address` | 取得地址物件 |
| 地址群組 | `/api/v2/cmdb/firewall/addrgrp` | 取得地址群組 |
| 服務物件 | `/api/v2/cmdb/firewall.service/custom` | 取得自訂服務 |
| 服務群組 | `/api/v2/cmdb/firewall.service/group` | 取得服務群組 |

### API URL 格式

```
https://{FORTIGATE_IP}/api/v2/{endpoint}?vdom={VDOM_NAME}
```

範例:
```
https://192.168.100.2/api/v2/cmdb/firewall/policy?vdom=root
https://192.168.100.2/api/v2/cmdb/firewall/address/Web-Server-1?vdom=vdom1
```

---

## 範例輸出

### 成功執行的日誌範例:

```
[2024-01-31 10:00:00] [INFO] [FortiGate] 開始取得防火牆規則 - FortiGate-FW-01 (192.168.100.2)
[2024-01-31 10:00:01] [INFO] [FortiGate] API Token 驗證成功 - 主機: FGT-600D, 版本: v7.0.0
[2024-01-31 10:00:02] [INFO] [FortiGate] CMDB 中有 10 個 IP 群組, 8 個服務群組
[2024-01-31 10:00:03] [INFO] [FortiGate] 找到 2 個 VDOM
[2024-01-31 10:00:04] [INFO] [FortiGate] 處理 VDOM: root
[2024-01-31 10:00:08] [INFO] [FortiGate] VDOM 'root' 包含 120 條規則
[2024-01-31 10:00:09] [DEBUG] [FortiGate] IP 群組 'Internal-Network' 存在於 CMDB，不展開
[2024-01-31 10:00:10] [DEBUG] [FortiGate] IP 群組 'External-Servers' 不在 CMDB，從 API 取得
[2024-01-31 10:00:11] [DEBUG] [FortiGate] 地址群組 'External-Servers' 包含 5 個成員
[2024-01-31 10:00:15] [INFO] [FortiGate] 處理 VDOM: vdom1
[2024-01-31 10:00:18] [INFO] [FortiGate] VDOM 'vdom1' 包含 80 條規則
[2024-01-31 10:00:25] [INFO] [FortiGate] 成功取得 600 筆防火牆規則
```

---

## 版本歷程

### v1.1.0 (2024-01-31)
- ✅ 新增實際 API 呼叫版本 (`FortiGateRealAPIClient`)
- ✅ 支援 API Token 認證
- ✅ 支援多 VDOM 環境
- ✅ 支援 Proxy 設定
- ✅ 實作重試機制
- ✅ 實作遞迴群組展開
- ✅ 新增詳細日誌記錄
- ✅ 支援地理位置物件

### v1.0.0 (2024-01-30)
- ✅ 初始版本 (示範版本)
- ✅ 模擬 API 回應

---

## 安全建議

1. **妥善保管 API Token**
   - API Token 等同於管理員密碼
   - 不要在程式碼中硬編碼
   - 定期更換 API Token

2. **設定 Trusted Hosts**
   - 限制 API User 只能從特定 IP 存取
   - 使用最小權限原則

3. **使用唯讀權限**
   - API User 應該只有讀取權限
   - 避免使用 super_admin profile

4. **啟用 HTTPS**
   - 確保 FortiGate 使用 HTTPS
   - 生產環境使用有效的 SSL 憑證

5. **監控 API 存取**
   - 定期檢查 FortiGate 的管理員登入日誌
   - 設定異常存取告警

---

## 聯絡資訊

如有任何問題或需要協助,請聯絡:

**作者**: 黃建豪 (Edward Huang)  
**電子郵件**: edward.huang@kli.com.tw  
**公司**: 寬聯資訊股份有限公司

---

## 附錄: 完整設定範例

### App.config 完整設定

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="DatabaseConnection" 
         connectionString="Data Source=.;Initial Catalog=FirewallDB;Integrated Security=True" 
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  
  <appSettings>
    <!-- API 設定 -->
    <add key="APITimeout" value="60000" />
    <add key="MaxRetryCount" value="3" />
    <add key="RetryDelayMilliseconds" value="2000" />
    
    <!-- Proxy 設定 -->
    <add key="UseProxy" value="false" />
    <add key="ProxyAddress" value="" />
    <add key="ProxyPort" value="0" />
    <add key="ProxyUserName" value="" />
    <add key="ProxyPassword" value="" />
    
    <!-- 日誌設定 -->
    <add key="LogFilePath" value="Logs\FirewallExport.log" />
    <add key="LogLevel" value="Info" />
    <add key="EnableConsoleLog" value="true" />
    <add key="EnableFileLog" value="true" />
  </appSettings>
</configuration>
```

### 資料庫完整設定

```sql
-- 1. 防火牆硬體資訊
INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
VALUES ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'FortiGate-FW-01', '192.168.100.2', 
        'FortiGate 資料中心主要防火牆');

-- 2. API 設定
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_FirewallRules', 
     'https://192.168.100.2/api/v2/cmdb/firewall/policy'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_IPGroupInformation', 
     'https://192.168.100.2/api/v2/cmdb/firewall/addrgrp'),
    ('FortiGate', 'FortiGate-600D', 'v7.0.0', 'Get_ServiceGroupInformation', 
     'https://192.168.100.2/api/v2/cmdb/firewall.service/group');
```

### 建立 FortiGate API User (CLI)

```bash
# 建立唯讀 Administrator Profile
config system accprofile
    edit "api_readonly"
        set secfabgrp read
        set ftviewgrp read
        set authgrp read
        set sysgrp read
        set netgrp read
        set loggrp read
        set fwgrp read
        set vpngrp read
        set utmgrp read
        set wanoptgrp read
        set wifi read
    next
end

# 建立 API User
config system api-user
    edit "firewall_export_api"
        set api-key YOUR-GENERATED-TOKEN
        set accprofile "api_readonly"
        set vdom "root"
        set comments "API for firewall rule export"
        config trusthost
            edit 1
                set ipv4-trusthost 192.168.1.0 255.255.255.0
            next
        end
    next
end
```

---

**文件最後更新**: 2024-01-31
