# 安裝與設定說明 / Installation Guide

## 前置準備 / Prerequisites

### 必要軟體 / Required Software
1. **Visual Studio 2017 或更新版本** / Visual Studio 2017 or later
2. **.NET Framework 4.7.2 或更新版本** / .NET Framework 4.7.2 or later
3. **SQL Server 2012 或更新版本** / SQL Server 2012 or later
4. **NuGet Package Manager**

## 安裝步驟 / Installation Steps

### 步驟 1: 安裝 NuGet 套件 / Step 1: Install NuGet Packages

在 Visual Studio 中開啟專案後,請透過 NuGet Package Manager 安裝以下套件:

Open the project in Visual Studio and install the following packages via NuGet Package Manager:

```powershell
Install-Package Dapper -Version 2.0.123
Install-Package Microsoft.Extensions.DependencyInjection -Version 6.0.0
Install-Package Newtonsoft.Json -Version 13.0.3
Install-Package System.Configuration.ConfigurationManager -Version 6.0.0
```

或使用 NuGet Package Manager Console 執行:
Or execute in NuGet Package Manager Console:

```powershell
Update-Package -Reinstall
```

### 步驟 2: 建立資料庫 / Step 2: Create Database

1. 開啟 SQL Server Management Studio
2. 執行資料庫建立腳本:
   - 檔案位置: `Database\CreateDatabase.sql`
   - 此腳本會建立 `FirewallDB` 資料庫及所有必要資料表
   - 並插入示範資料

Open SQL Server Management Studio and execute:
- File location: `Database\CreateDatabase.sql`
- This script will create the `FirewallDB` database and all required tables
- It also inserts demo data

### 步驟 3: 設定連線字串 / Step 3: Configure Connection Strings

編輯 `App.config` 檔案,修改資料庫連線字串:

Edit the `App.config` file to update the database connection strings:

```xml
<connectionStrings>
  <add name="DatabaseConnection" 
       connectionString="Data Source=YOUR_SERVER_NAME;Initial Catalog=FirewallDB;Integrated Security=True" 
       providerName="System.Data.SqlClient" />
  <add name="CMDBConnection" 
       connectionString="Data Source=YOUR_SERVER_NAME;Initial Catalog=CMDB;Integrated Security=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**注意**: 請將 `YOUR_SERVER_NAME` 替換為您的 SQL Server 實例名稱
Note: Replace `YOUR_SERVER_NAME` with your SQL Server instance name

範例 / Examples:
- 本機預設實例 / Local default instance: `.` 或 `localhost`
- 具名實例 / Named instance: `.\SQLEXPRESS` 或 `localhost\SQLEXPRESS`
- 遠端伺服器 / Remote server: `192.168.1.100` 或 `server.domain.com`

### 步驟 4: 建置專案 / Step 4: Build the Project

在 Visual Studio 中:
1. 右鍵點選解決方案
2. 選擇「重建方案」(Rebuild Solution)
3. 確認建置成功,沒有錯誤訊息

In Visual Studio:
1. Right-click on the solution
2. Select "Rebuild Solution"
3. Verify the build succeeds without errors

### 步驟 5: 執行程式 / Step 5: Run the Application

1. 按 F5 或點選「開始偵錯」執行程式
2. 程式會自動:
   - 初始化日誌系統
   - 從資料庫讀取防火牆裝置清單
   - 依序處理每個防火牆裝置
   - 取得防火牆規則
   - 將規則儲存至資料庫

1. Press F5 or click "Start Debugging" to run the application
2. The application will automatically:
   - Initialize the logging system
   - Read the firewall device list from the database
   - Process each firewall device in sequence
   - Retrieve firewall rules
   - Save rules to the database

## 驗證安裝 / Verification

### 檢查日誌檔案 / Check Log Files
查看 `Logs\FirewallExport.log` 檔案,確認程式執行狀況

View the `Logs\FirewallExport.log` file to verify execution status

### 檢查資料庫 / Check Database
執行以下 SQL 查詢,確認規則已匯入:

Execute the following SQL query to verify rules were imported:

```sql
USE FirewallDB;
GO

-- 檢查防火牆規則筆數 / Check firewall rule count
SELECT DEVICE_NAME, COUNT(*) as RuleCount
FROM FIREWALL_RULE_SET
GROUP BY DEVICE_NAME;

-- 查看最新匯入的規則 / View recently imported rules
SELECT TOP 10 *
FROM FIREWALL_RULE_SET
ORDER BY DATA_DT DESC;
```

## 故障排除 / Troubleshooting

### 問題 1: NuGet 套件無法安裝
**解決方法**:
1. 確認網路連線正常
2. 清除 NuGet 快取: `nuget locals all -clear`
3. 重新啟動 Visual Studio
4. 再次嘗試安裝套件

### Problem 1: Cannot Install NuGet Packages
**Solution**:
1. Verify internet connection
2. Clear NuGet cache: `nuget locals all -clear`
3. Restart Visual Studio
4. Try installing packages again

### 問題 2: 資料庫連線失敗
**可能原因與解決方法**:
- SQL Server 服務未啟動 → 啟動 SQL Server 服務
- 連線字串錯誤 → 檢查 App.config 中的連線字串
- 權限不足 → 確認使用者有資料庫存取權限
- 防火牆阻擋 → 檢查 Windows 防火牆設定

### Problem 2: Database Connection Failure
**Possible Causes and Solutions**:
- SQL Server service not running → Start SQL Server service
- Incorrect connection string → Check connection string in App.config
- Insufficient permissions → Verify user has database access
- Firewall blocking → Check Windows Firewall settings

### 問題 3: 編譯錯誤
**解決方法**:
1. 確認所有 NuGet 套件已正確安裝
2. 清理方案: Build → Clean Solution
3. 重建方案: Build → Rebuild Solution
4. 如仍有錯誤,刪除 `bin` 和 `obj` 資料夾後重新建置

### Problem 3: Compilation Errors
**Solution**:
1. Verify all NuGet packages are installed correctly
2. Clean solution: Build → Clean Solution
3. Rebuild solution: Build → Rebuild Solution
4. If errors persist, delete `bin` and `obj` folders and rebuild

### 問題 4: 找不到防火牆裝置
**說明**: 程式會自動使用示範資料
**如需使用實際資料**:
1. 在 `FW_HW_INFO` 資料表中新增防火牆裝置資訊
2. 在 `FW_API_INFO` 資料表中新增對應的 API 設定
3. 重新執行程式

### Problem 4: No Firewall Devices Found
**Note**: The application will automatically use demo data
**To use actual data**:
1. Add firewall device information to `FW_HW_INFO` table
2. Add corresponding API settings to `FW_API_INFO` table
3. Run the application again

## 設定選項 / Configuration Options

### App.config 設定說明 / App.config Settings

```xml
<appSettings>
  <!-- API 連線逾時時間 (毫秒) / API connection timeout (milliseconds) -->
  <add key="APITimeout" value="30000" />
  
  <!-- API 最大重試次數 / API maximum retry count -->
  <add key="MaxRetryCount" value="3" />
  
  <!-- API 重試延遲時間 (毫秒) / API retry delay (milliseconds) -->
  <add key="RetryDelayMilliseconds" value="1000" />
  
  <!-- 是否使用 Proxy / Use proxy server -->
  <add key="UseProxy" value="false" />
  
  <!-- Proxy 位址 / Proxy address -->
  <add key="ProxyAddress" value="" />
  
  <!-- Proxy 連接埠 / Proxy port -->
  <add key="ProxyPort" value="0" />
  
  <!-- 日誌檔案路徑 / Log file path -->
  <add key="LogFilePath" value="Logs\FirewallExport.log" />
  
  <!-- 日誌等級 (Trace, Debug, Info, Warning, Error) / Log level -->
  <add key="LogLevel" value="Info" />
  
  <!-- 啟用主控台日誌 / Enable console logging -->
  <add key="EnableConsoleLog" value="true" />
  
  <!-- 啟用檔案日誌 / Enable file logging -->
  <add key="EnableFileLog" value="true" />
</appSettings>
```

## 進階設定 / Advanced Configuration

### 自訂 CMDB 實作 / Custom CMDB Implementation

如需連接您自己的 CMDB 系統,請實作 `ICMDBSource` 介面:

To connect to your own CMDB system, implement the `ICMDBSource` interface:

1. 建立新類別繼承 `ICMDBSource`
2. 實作所有必要方法
3. 在 `Program.cs` 的 `ConfigureServices` 方法中註冊您的實作

1. Create a new class implementing `ICMDBSource`
2. Implement all required methods
3. Register your implementation in the `ConfigureServices` method in `Program.cs`

### 新增防火牆廠牌支援 / Add New Firewall Vendor Support

如需支援新的防火牆廠牌,請實作 `IFirewallAPIClient` 介面:

To support a new firewall vendor, implement the `IFirewallAPIClient` interface:

1. 建立新類別繼承 `IFirewallAPIClient`
2. 實作 `GetFirewallRules()` 方法
3. 在 `DemoCMDBSource.DoFirewallRules()` 中新增對應邏輯

1. Create a new class implementing `IFirewallAPIClient`
2. Implement the `GetFirewallRules()` method
3. Add corresponding logic in `DemoCMDBSource.DoFirewallRules()`

## 技術支援 / Technical Support

如有任何問題或需要協助,請聯絡:

For any questions or assistance, please contact:

**姓名 / Name**: 黃建豪 / Edward Huang  
**電子郵件 / Email**: edward.huang@kli.com.tw  
**公司 / Company**: 寬聯資訊股份有限公司 / Kuan Lian Information Co., Ltd.

## 附錄 / Appendix

### 必要的 NuGet 套件清單 / Required NuGet Packages List

| 套件名稱 / Package Name | 版本 / Version | 用途 / Purpose |
|------------------------|---------------|---------------|
| Dapper | 2.0.123 | ORM 框架 / ORM Framework |
| Microsoft.Extensions.DependencyInjection | 6.0.0 | 依賴注入 / Dependency Injection |
| Newtonsoft.Json | 13.0.3 | JSON 處理 / JSON Processing |
| System.Configuration.ConfigurationManager | 6.0.0 | 設定檔管理 / Configuration Management |

### 資料庫需求 / Database Requirements

- **SQL Server 版本** / SQL Server Version: 2012 或更新 / 2012 or later
- **資料庫大小預估** / Estimated Database Size: 視規則數量而定,建議至少 100 MB / Depends on rule count, recommend at least 100 MB
- **必要權限** / Required Permissions:
  - CREATE DATABASE (初次設定時) / (For initial setup)
  - CREATE TABLE
  - SELECT, INSERT, UPDATE, DELETE (執行時) / (At runtime)
