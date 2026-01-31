# CheckPoint 防火牆 API 實作說明
# CheckPoint Firewall API Implementation Guide

## 概述 / Overview

本專案提供兩種 CheckPoint 防火牆 API 客戶端實作:

1. **CheckPointFirewallAPIClient.cs** - 示範版本 (使用模擬資料)
2. **CheckPointRealAPIClient.cs** - 實際版本 (真實 API 呼叫) ⭐ **NEW**

## 實際 API 客戶端特色 / Real API Client Features

### ✅ 完整功能
- ✔️ 支援帳號密碼認證
- ✔️ 支援 API Key/Token 認證 (建議使用)
- ✔️ 自動 Session 管理
- ✔️ SSL 憑證處理
- ✔️ Proxy 支援
- ✔️ 重試機制
- ✔️ 完整的錯誤處理
- ✔️ 詳細的日誌記錄

### ✅ API 功能
- ✔️ 取得所有 Policy Packages
- ✔️ 取得 Access Rulebase (分頁處理)
- ✔️ 取得 IP 群組及成員 (遞迴展開)
- ✔️ 取得服務群組及成員 (遞迴展開)
- ✔️ 取得規則命中統計

### ✅ 智慧展開邏輯
- ✔️ 優先使用 CMDB 中的群組定義
- ✔️ 自動從 API 取得不在 CMDB 中的群組
- ✔️ 支援巢狀群組 (遞迴展開)
- ✔️ 笛卡爾乘積展開規則

---

## 支援的 CheckPoint 物件類型

### IP 物件類型
| 類型 | CheckPoint Type | 說明 | 範例 |
|------|----------------|------|------|
| 主機 | `host` | 單一 IP 位址 | 192.168.1.100 |
| 網路 | `network` | 子網路 | 192.168.1.0/24 |
| IP 範圍 | `address-range` | IP 範圍 | 192.168.1.1-192.168.1.254 |
| 網域 | `dns-domain` | FQDN | *.example.com |
| 群組 | `group` | IP 群組 | Internal-Network |
| 任意 | `CpmiAnyObject` | 任意位址 | Any |

### 服務物件類型
| 類型 | CheckPoint Type | 說明 | 範例 |
|------|----------------|------|------|
| TCP 服務 | `service-tcp` | TCP 埠號 | 80, 443, 8080-8090 |
| UDP 服務 | `service-udp` | UDP 埠號 | 53, 123 |
| ICMP 服務 | `service-icmp` | ICMP 類型 | ping |
| 其他服務 | `service-other` | 其他協定 | GRE, ESP |
| 服務群組 | `service-group` | 服務群組 | Web-Services |
| 任意 | `CpmiAnyObject` | 任意服務 | Any |

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
        // 使用實際 API 版本
        client = new CheckPointRealAPIClient(device, this);
        
        // 或使用示範版本
        // client = new CheckPointFirewallAPIClient(device, this);
    }
    else if (device.Brand.ToUpper() == "FORTIGATE")
    {
        client = new FortiGateFirewallAPIClient(device, this);
    }
    else
    {
        throw new NotSupportedException($"不支援的防火牆廠牌: {device.Brand}");
    }

    return await client.GetFirewallRules();
}
```

### 方法 2: 建立新的 CMDB 實作

```csharp
public class ProductionCMDBSource : ICMDBSource
{
    public async Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device)
    {
        if (device.Brand.ToUpper() == "CHECKPOINT")
        {
            var client = new CheckPointRealAPIClient(device, this);
            return await client.GetFirewallRules();
        }
        // ... 其他廠牌
    }
    
    // ... 其他方法實作
}
```

然後在 `Program.cs` 中註冊:

```csharp
private static ServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // 使用生產環境 CMDB
    services.AddSingleton<ICMDBSource, ProductionCMDBSource>();
    
    return services.BuildServiceProvider();
}
```

---

## 認證設定 / Authentication Setup

### 選項 1: 使用 API Key (建議) ⭐

API Key 是最安全的認證方式,不需要在程式中儲存密碼。

#### 在 CheckPoint Management Server 上建立 API Key:

1. 登入 SmartConsole
2. 前往 **Manage & Settings** → **Permissions & Administrators**
3. 選擇管理員帳號
4. 點選 **Edit** → **Authentication** tab
5. 啟用 **API Key Authentication**
6. 複製產生的 API Key

#### 在資料庫中設定 (建議方式):

```sql
-- 假設您有一個儲存 API 認證的資料表
UPDATE FW_API_CREDENTIALS
SET TOKEN = 'your-api-key-here',
    AUTH_TYPE = 'Token'
WHERE DEVICE_NAME = 'CheckPoint-FW-01';
```

#### 或在 DemoCMDBSource 中設定:

```csharp
public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
{
    if (device.Brand.ToUpper() == "CHECKPOINT")
    {
        return new ApiUserInfo
        {
            AuthType = "Token",
            Token = "your-checkpoint-api-key-here",
            // UserName 和 Password 留空
        };
    }
    // ... 其他廠牌
}
```

### 選項 2: 使用帳號密碼

```csharp
public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
{
    if (device.Brand.ToUpper() == "CHECKPOINT")
    {
        return new ApiUserInfo
        {
            UserName = "admin",
            Password = "your-password",
            AuthType = "Password"
            // Token 留空
        };
    }
    // ... 其他廠牌
}
```

⚠️ **安全提醒**: 
- 不要在程式碼中硬編碼密碼
- 使用環境變數或安全的金鑰管理系統
- 優先使用 API Key 認證

---

## API URL 設定

### 自動設定 (預設)

如果資料庫中沒有設定 API URL,程式會自動使用預設 URL:

```
https://{DEVICE_IP}/web_api/{endpoint}
```

### 手動設定 (建議)

在 `FW_API_INFO` 資料表中設定:

```sql
-- CheckPoint R80.40 API URLs
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('CheckPoint', 'R80.40', 'R80.40', 'Login_Session', 
     'https://192.168.100.1/web_api/login'),
    
    ('CheckPoint', 'R80.40', 'R80.40', 'Logout_Session', 
     'https://192.168.100.1/web_api/logout'),
    
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallRules', 
     'https://192.168.100.1/web_api/show-access-rulebase'),
    
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_IPGroupInformation', 
     'https://192.168.100.1/web_api/show-group'),
    
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_ServiceGroupInformation', 
     'https://192.168.100.1/web_api/show-service-group'),
    
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallPolicyPackages', 
     'https://192.168.100.1/web_api/show-packages');
```

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

修改 `CheckPointRealAPIClient.cs`:

```csharp
// 移除或註解掉這一行
// handler.ServerCertificateCustomValidationCallback = 
//     (sender, cert, chain, sslPolicyErrors) => true;

// 確保 CheckPoint Management Server 使用有效的 SSL 憑證
// 或將憑證安裝到信任的根憑證授權單位
```

---

## Proxy 設定

如果您的環境需要透過 Proxy 連線到 CheckPoint:

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

### 或在 DemoCMDBSource 中設定:

```csharp
public bool GetUseProxy() => true;
public string GetProxyAddress() => "proxy.company.com";
public int GetProxyPort() => 8080;
public string GetProxyUserName() => "proxy_user";
public string GetProxyPassword() => "proxy_password";
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

### 規則分頁設定

修改 `CheckPointRealAPIClient.cs` 中的分頁大小:

```csharp
int limit = 50; // 預設每次取 50 筆規則
```

根據您的 CheckPoint 伺服器效能調整:
- 效能較好: 可增加到 100-200
- 效能較差: 降低到 20-30

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

會顯示完整的 API 請求和回應內容 (注意:可能包含敏感資訊):

```xml
<add key="LogLevel" value="Trace" />
```

⚠️ **生產環境建議使用 Info 等級**

---

## API 限制與注意事項

### CheckPoint API 限制

1. **連線數限制**: 
   - 預設同時連線數: 5-10 個
   - 建議序列處理多個防火牆

2. **Session 超時**:
   - 預設 Session 閒置超時: 10 分鐘
   - 建議處理完立即登出

3. **查詢數量限制**:
   - 單次查詢最大筆數: 500 筆
   - 程式已實作分頁處理

4. **API 版本差異**:
   - R80.x 和 R81.x API 路徑略有不同
   - 請確認您的版本並設定正確的 API URL

### 權限需求

執行此程式的 API 使用者需要以下權限:
- ✅ **Read-Only** 權限即可
- ✅ 存取 **Policy** 物件
- ✅ 存取 **Network Objects**
- ✅ 存取 **Service Objects**
- ✅ 查看 **Hit Count** (若需要命中統計)

---

## 故障排除 / Troubleshooting

### 問題 1: 連線逾時

**症狀**: `A task was canceled` 或 `The operation has timed out`

**解決方法**:
1. 檢查網路連線到 CheckPoint Management Server
2. 增加 `APITimeout` 設定值
3. 檢查防火牆規則是否允許連線
4. 確認 Management Server 的 Web API 服務正在運行

### 問題 2: 認證失敗

**症狀**: `Invalid credentials` 或 `Authentication failed`

**解決方法**:
1. 確認 API Key 或密碼正確
2. 確認帳號有足夠的權限
3. 檢查 API Key 是否過期
4. 嘗試使用 SmartConsole 登入驗證帳號狀態

### 問題 3: SSL 憑證錯誤

**症狀**: `The SSL connection could not be established`

**解決方法**:
1. 開發環境: 確認憑證驗證已停用 (預設已停用)
2. 生產環境: 安裝 CheckPoint 的 SSL 憑證到系統信任區
3. 或使用有效的公開憑證

### 問題 4: 未找到 Policy Package

**症狀**: `No policy packages found`

**解決方法**:
1. 確認 CheckPoint 上至少有一個 Policy Package
2. 確認 API 使用者有權限存取 Policy
3. 檢查日誌查看詳細錯誤訊息

### 問題 5: 規則數量不符

**症狀**: 取得的規則數量少於 SmartConsole 中看到的

**可能原因**:
1. 某些規則在不同的 Layer 中
2. 某些規則被停用
3. 某些規則在不同的 Policy Package 中

**解決方法**:
- 檢查日誌確認處理了哪些 Policy Package
- 確認是否需要處理多個 Layer

---

## 效能參考數據

基於測試環境的效能數據:

| 規則數量 | 處理時間 | 備註 |
|---------|---------|------|
| 100 條規則 | ~10 秒 | 不包含群組展開 |
| 100 條規則 | ~30 秒 | 包含 10 個群組展開 |
| 500 條規則 | ~50 秒 | 不包含群組展開 |
| 500 條規則 | ~2 分鐘 | 包含 50 個群組展開 |
| 1000 條規則 | ~3 分鐘 | 包含群組展開 |

**影響效能的因素**:
- CheckPoint Management Server 效能
- 網路延遲
- 群組巢狀層級
- 規則複雜度
- 是否啟用命中統計

---

## API 文件參考

### 官方文件
- **CheckPoint API Reference**: https://sc1.checkpoint.com/documents/latest/APIs/
- **Getting Started**: https://sc1.checkpoint.com/documents/latest/APIs/#introduction~v1.9
- **Authentication**: https://sc1.checkpoint.com/documents/latest/APIs/#web/login~v1.9

### 常用 API 端點

| 功能 | API 端點 | 說明 |
|------|---------|------|
| 登入 | `/web_api/login` | 建立 API Session |
| 登出 | `/web_api/logout` | 結束 API Session |
| 查詢規則 | `/web_api/show-access-rulebase` | 取得 Access Rules |
| 查詢群組 | `/web_api/show-group` | 取得 IP 群組 |
| 查詢服務群組 | `/web_api/show-service-group` | 取得服務群組 |
| 查詢 Packages | `/web_api/show-packages` | 取得所有 Policy Packages |

---

## 範例輸出

### 成功執行的日誌範例:

```
[2024-01-31 10:00:00] [INFO] [CheckPoint] 開始取得防火牆規則 - CheckPoint-FW-01 (192.168.100.1)
[2024-01-31 10:00:01] [INFO] [CheckPoint] 登入成功 - Session ID: 12345678...
[2024-01-31 10:00:02] [INFO] [CheckPoint] CMDB 中有 10 個 IP 群組, 8 個服務群組
[2024-01-31 10:00:03] [INFO] [CheckPoint] 找到 1 個 Policy Package
[2024-01-31 10:00:04] [INFO] [CheckPoint] 處理 Policy Package: Standard
[2024-01-31 10:00:10] [INFO] [CheckPoint] Package 'Standard' 包含 150 條規則
[2024-01-31 10:00:15] [DEBUG] [CheckPoint] IP 群組 'Internal-Network' 存在於 CMDB，不展開
[2024-01-31 10:00:16] [DEBUG] [CheckPoint] IP 群組 'External-Servers' 不在 CMDB，從 API 取得
[2024-01-31 10:00:17] [DEBUG] [CheckPoint] IP 群組 'External-Servers' 包含 5 個成員
[2024-01-31 10:00:30] [INFO] [CheckPoint] 登出成功
[2024-01-31 10:00:31] [INFO] [CheckPoint] 成功取得 450 筆防火牆規則
```

---

## 版本歷程

### v1.1.0 (2024-01-31)
- ✅ 新增實際 API 呼叫版本 (`CheckPointRealAPIClient`)
- ✅ 支援 API Key 認證
- ✅ 支援 Proxy 設定
- ✅ 實作重試機制
- ✅ 實作遞迴群組展開
- ✅ 實作分頁查詢
- ✅ 新增詳細日誌記錄

### v1.0.0 (2024-01-30)
- ✅ 初始版本 (示範版本)
- ✅ 模擬 API 回應

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
VALUES ('CheckPoint', 'R80.40', 'R80.40', 'CheckPoint-FW-01', '192.168.100.1', 
        'CheckPoint 總部防火牆');

-- 2. API 設定
INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES 
    ('CheckPoint', 'R80.40', 'R80.40', 'Login_Session', 
     'https://192.168.100.1/web_api/login'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Logout_Session', 
     'https://192.168.100.1/web_api/logout'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallRules', 
     'https://192.168.100.1/web_api/show-access-rulebase'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_IPGroupInformation', 
     'https://192.168.100.1/web_api/show-group'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_ServiceGroupInformation', 
     'https://192.168.100.1/web_api/show-service-group'),
    ('CheckPoint', 'R80.40', 'R80.40', 'Get_FirewallPolicyPackages', 
     'https://192.168.100.1/web_api/show-packages');
```

---

**文件最後更新**: 2024-01-31
