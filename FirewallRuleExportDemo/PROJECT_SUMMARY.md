# 防火牆規則匯出專案 - 完整說明文件

## 專案概述

本專案為防火牆規則匯出主控台應用程式的示範版本(DEMO),設計目的是從各種防火牆廠牌的管理介面中自動擷取防火牆規則,並將這些規則標準化後儲存至資料庫中。專案採用介面導向設計,具有高度的可擴充性與彈性。

## 專案結構

```
FirewallRuleExportDemo/
├── Enums/
│   └── APINames.cs                     # API名稱列舉定義
├── Interfaces/
│   ├── IApiUserInfo.cs                 # API使用者資訊介面
│   ├── ICMDBSource.cs                  # CMDB資料來源介面
│   ├── IFIREWALL_RULE_SET.cs          # 防火牆規則資料介面
│   ├── IFirewallAPI.cs                 # 防火牆API資訊介面
│   ├── IFirewallAPIClient.cs           # 防火牆API客戶端介面
│   ├── IFirewallDeviceInfo.cs          # 防火牆裝置資訊介面
│   ├── IIPGroupInformation.cs          # IP群組資訊介面
│   └── IServiceInformation.cs          # 服務群組資訊介面
├── Models/
│   ├── ApiUserInfo.cs                  # API使用者資訊模型
│   ├── FirewallAPI.cs                  # 防火牆API資訊模型
│   ├── FirewallDeviceInfo.cs           # 防火牆裝置資訊模型
│   ├── FirewallRuleSet.cs              # 防火牆規則模型
│   ├── IPGroupInformation.cs           # IP群組資訊模型
│   └── ServiceInformation.cs           # 服務群組資訊模型
├── Implementation/
│   ├── CheckPointFirewallAPIClient.cs  # CheckPoint防火牆API客戶端實作
│   ├── DemoCMDBSource.cs               # 示範CMDB資料來源實作
│   └── FortiGateFirewallAPIClient.cs   # FortiGate防火牆API客戶端實作
├── Services/
│   └── FirewallRuleExporter.cs         # 防火牆規則匯出服務
├── Utilities/
│   └── Logger.cs                       # 日誌記錄工具
├── Database/
│   └── CreateDatabase.sql              # 資料庫建立腳本
├── Properties/
│   └── AssemblyInfo.cs                 # 組件資訊
├── App.config                          # 應用程式設定檔
├── Program.cs                          # 主程式進入點
├── packages.config                     # NuGet套件設定
├── Install-Packages.ps1                # NuGet套件安裝腳本
├── README.md                           # 專案說明文件(英文)
├── ReadMe.txt                          # 專案說明文件(中文)
├── INSTALLATION.md                     # 安裝說明文件
└── PROJECT_SUMMARY.md                  # 本文件
```

## 核心功能

### 1. 多廠牌防火牆支援
- **CheckPoint**: 支援 R80.x 以上版本
- **FortiGate**: 支援 6.x 以上版本
- 可透過實作 `IFirewallAPIClient` 介面來擴充更多廠牌

### 2. 規則展開邏輯
程式會自動處理防火牆規則中的群組物件:
- **IP群組展開**: 將IP群組展開為個別的IP位址或子網路
- **服務群組展開**: 將服務群組展開為個別的通訊協定與埠號
- **智慧比對**: 優先使用CMDB中已定義的群組,避免不必要的API呼叫

### 3. CMDB整合
- 支援從CMDB系統讀取防火牆裝置清單
- 支援從CMDB系統讀取API認證資訊
- 支援從CMDB系統讀取IP群組與服務群組定義
- 可透過實作 `ICMDBSource` 介面來整合不同的CMDB系統

### 4. 資料持久化
- 使用Dapper ORM框架進行資料存取
- 自動將防火牆規則儲存至SQL Server資料庫
- 支援批次寫入以提升效能

### 5. 依賴注入(DI)
- 使用Microsoft.Extensions.DependencyInjection框架
- 易於進行單元測試
- 易於擴充與維護

### 6. 完整的日誌記錄
- 支援多種日誌等級(Trace, Debug, Info, Warning, Error)
- 同時支援主控台與檔案輸出
- 詳細記錄程式執行過程與錯誤資訊

## 資料庫架構

### 核心資料表

#### FIREWALL_RULE_SET (防火牆規則資料表)
儲存所有從防火牆匯出的規則資料,包含:
- 來源/目的IP資訊(群組、IP範圍、子網路、FQDN)
- 服務資訊(協定、埠號、服務群組)
- 規則動作(允許/拒絕)
- 命中統計資訊
- 規則狀態與備註

#### FW_HW_INFO (防火牆硬體資訊資料表)
儲存防火牆裝置的基本資訊:
- 廠牌、型號、韌體版本
- 裝置名稱與IP位址
- 描述說明

#### FW_API_INFO (防火牆API資訊資料表)
儲存各廠牌防火牆的API設定:
- API名稱與URL
- 對應的防火牆廠牌、型號、版本

#### IP群組相關資料表
- **IP_GROUP_MAIN**: IP群組主表
- **IP_GROUP_DETAILS**: IP群組明細表

#### 服務群組相關資料表
- **SERVICE_GROUP_MAIN**: 服務群組主表
- **SERVICE_GROUP_DETAILS**: 服務群組明細表

## 程式流程

```
1. 程式啟動
   ↓
2. 初始化DI容器與日誌系統
   ↓
3. 從CMDB取得防火牆裝置清單
   ↓
4. 針對每個防火牆裝置:
   a. 根據廠牌建立對應的API客戶端
   b. 從CMDB取得API認證資訊
   c. 呼叫API取得防火牆規則
   d. 從CMDB取得IP群組與服務群組資訊
   e. 展開規則中的群組物件
   f. 將規則儲存至資料庫
   ↓
5. 發送完成通知
   ↓
6. 程式結束
```

## 主要類別說明

### Program.cs
- 程式進入點
- 設定DI容器
- 初始化日誌系統
- 協調整體執行流程

### FirewallRuleExporter.cs
- 防火牆規則匯出的核心邏輯
- 協調API客戶端與CMDB資料來源
- 處理例外狀況與日誌記錄

### DemoCMDBSource.cs
- `ICMDBSource` 介面的示範實作
- 提供資料庫存取功能
- 管理設定資訊的讀取

### CheckPointFirewallAPIClient.cs
- CheckPoint防火牆API客戶端實作
- 處理CheckPoint特有的API呼叫方式
- 實作規則展開邏輯

### FortiGateFirewallAPIClient.cs
- FortiGate防火牆API客戶端實作
- 處理FortiGate特有的API呼叫方式
- 實作規則展開邏輯

### Logger.cs
- 提供統一的日誌記錄功能
- 支援多種輸出目標與日誌等級
- 執行緒安全的日誌寫入

## 擴充指南

### 新增防火牆廠牌支援

1. **建立新的API客戶端類別**
```csharp
public class PaloAltoFirewallAPIClient : IFirewallAPIClient
{
    public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
    {
        // 實作取得防火牆規則的邏輯
    }
    
    public IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device)
    {
        return new PaloAltoFirewallAPIClient(device, _cmdbSource);
    }
}
```

2. **在DemoCMDBSource中註冊新廠牌**
```csharp
public async Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device)
{
    IFirewallAPIClient client = null;

    if (device.Brand.ToUpper() == "PALOALTO")
    {
        client = new PaloAltoFirewallAPIClient(device, this);
    }
    // ... 其他廠牌
    
    return await client.GetFirewallRules();
}
```

3. **在資料庫中新增對應設定**
```sql
INSERT INTO FW_HW_INFO (BRAND, MODEL, FW_VERSION, DEVICE_NAME, DEVICE_IP, DESCRIPTION)
VALUES ('PaloAlto', 'PA-5220', 'v10.0', 'PA-FW-01', '192.168.100.3', 'PaloAlto防火牆');

INSERT INTO FW_API_INFO (BRAND, MODEL, FW_VERSION, API_NAME, API_URL)
VALUES ('PaloAlto', 'PA-5220', 'v10.0', 'Get_FirewallRules', 'https://192.168.100.3/api/');
```

### 整合自訂CMDB系統

1. **實作ICMDBSource介面**
```csharp
public class CustomCMDBSource : ICMDBSource
{
    public List<IFirewallDeviceInfo> GetFirewallList()
    {
        // 從您的CMDB系統取得防火牆清單
    }
    
    public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
    {
        // 從您的CMDB系統取得API認證資訊
    }
    
    // 實作其他必要方法...
}
```

2. **在Program.cs中註冊**
```csharp
private static ServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    services.AddSingleton<ICMDBSource, CustomCMDBSource>();
    return services.BuildServiceProvider();
}
```

## 效能考量

### 批次處理
- 規則展開採用記憶體內處理,避免過多的資料庫查詢
- 使用Dapper批次插入功能,一次寫入多筆規則

### 快取機制
- CMDB資料(IP群組、服務群組)在程式執行期間快取於記憶體中
- 減少重複的API呼叫

### 非同步處理
- 所有I/O操作都使用非同步方式執行
- 提升程式執行效率

## 安全性考量

### 認證資訊保護
- API認證資訊應從安全的CMDB系統取得
- 避免在程式碼中硬編碼認證資訊
- 建議使用加密連線(HTTPS)與防火牆API通訊

### 資料庫安全
- 使用參數化查詢防止SQL注入攻擊
- 建議使用Windows整合認證或加密的SQL認證
- 限制資料庫使用者的權限(最小權限原則)

### 日誌資訊
- 避免在日誌中記錄敏感資訊(如密碼、Token)
- 定期清理舊的日誌檔案
- 限制日誌檔案的存取權限

## 測試建議

### 單元測試
- 針對各個API客戶端類別撰寫單元測試
- 模擬CMDB資料來源進行測試
- 測試規則展開邏輯的正確性

### 整合測試
- 測試與實際防火牆API的連線
- 測試資料庫寫入功能
- 測試完整的執行流程

### 效能測試
- 測試大量規則的處理效能
- 測試多個防火牆同時處理的效能
- 監控記憶體使用情況

## 常見問題(FAQ)

### Q1: 如何處理防火牆API速率限制?
A1: 在`App.config`中調整`APITimeout`與重試相關設定。必要時可在API客戶端中實作速率限制邏輯。

### Q2: 規則數量很大時,如何提升效能?
A2: 
- 調整資料庫批次寫入的數量
- 考慮使用非同步並行處理多個防火牆
- 在資料庫中建立適當的索引

### Q3: 如何處理防火牆API的版本差異?
A3: 為不同版本建立不同的API客戶端實作,或在API客戶端中根據版本選擇不同的處理邏輯。

### Q4: 是否支援排程自動執行?
A4: 本程式為主控台應用程式,可透過Windows工作排程器或其他排程工具定期執行。

### Q5: 如何處理防火牆規則的更新?
A5: 程式執行時會先刪除該防火牆的現有規則,再寫入新規則,確保資料為最新狀態。

## 版本歷程

### v1.0.0 (2024)
- 初始DEMO版本
- 支援CheckPoint與FortiGate防火牆
- 基本的規則展開功能
- 資料庫整合
- 日誌記錄功能

## 授權資訊

本專案為示範版本(DEMO),如需商業使用請聯絡作者取得授權。

## 聯絡資訊

**作者**: 黃建豪 (Edward Huang)  
**電子郵件**: edward.huang@kli.com.tw  
**公司**: 寬聯資訊股份有限公司

## 致謝

感謝所有參與本專案開發與測試的人員。

---

本文件最後更新: 2024年1月
