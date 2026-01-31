# 防火牆規則匯出主控台應用程式(DEMO)

# 特色
## 定義不同介面以支援多種實作擴充:
### 有定義一個 ICMDBSource介面 可以實作該介面以對應不同CMDB系統
### 有定義一個 IFirewallAPIClient介面 可以對應不同廠牌防火牆管理介面的API實作對應
### 有定義一個資料介面 IFIREWALL_RULE_SET 可以對應不同資料表的實作
### 有定義一個 IUserInfo 介面用來做儲存登入API的使用者資訊,可以透過 ICMDBSource 的 GetAPIUser公開方法取得該資訊
### 有定義一個 IIPGroupInformation 介面用來儲存IP群組設定的不同實作
### 有定義一個 IServiceInformation 介面用來儲存應用程式服務群組設定的不同實作
## 使用ORM框架Dapper來做資料存取
## 可支援DI以易於擴充支援更多廠牌的API
## 提供示範版本及實際 API 版本兩種實作供選擇

# 架構

## 資料表結構

### FIREWALL_RULE_SET

	[DEVICE_NAME]          NVARCHAR (100) NOT NULL,		--防火牆名稱(PK)
    [RULENO]               NVARCHAR (100) NOT NULL,		--規則編號(PK)
    [UUID]                 NVARCHAR (40)  NOT NULL,		--規則UUID(PK)
    [SAMEKEY]              INT            NOT NULL,		--相同鍵值(PK)
	[INCOMING_INTERFACE]	NVARCHAR(100) NULL,			--來源接口
	[OUTGOING_INTERFACE]	NVARCHAR(100) NULL,			--目的接口
	[SOURCE_GROUP] NVARCHAR(100) NULL,					--來源群組
    [SOURCE_IP_START]      NVARCHAR (100) NULL,			--來源IP起始
    [SOURCE_IP_END]        NVARCHAR (100) NULL,			--來源IP結束
    [SOURCE_FQDN]          NVARCHAR (500) NULL,			--來源FQDN
    [SOURCE_SUBNET]        NVARCHAR (100) NULL,			--來源子網
	[DESTINATION_GROUP] NVARCHAR(100) NULL,				--目的群組
    [DESTINATION_IP_START] NVARCHAR (100) NULL,			--目的IP起始
    [DESTINATION_IP_END]   NVARCHAR (100) NULL,			--目的IP結束
    [DESTINATION_FQDN]     NVARCHAR (500) NULL,			--目的FQDN
    [DESTINATION_SUBNET1]  NVARCHAR (100) NULL,			--目的子網
    [PROTOCAL]             NVARCHAR (100) NOT NULL,		--協定
    [PORT_S]               INT            NULL,			--起始埠
    [PORT_E]               INT            NULL,			--結束埠
    [SERVICE_GROUP] NVARCHAR(100) NULL,					--服務群組
	[ACTION]				NVARCHAR(100) NOT NULL,		--行為
	[ENABLED]              BIT            CONSTRAINT [DF_FIREWALL_RULE_SET_ENABLED] DEFAULT ((1)) NOT NULL,	--是否啟用
    [DATA_DT]              NVARCHAR (50)  NULL,			--資料日期
    [HIT]                  INT            NULL,			--命中次數
    [FIRST_HIT_DATE]       NVARCHAR (50)  NULL,			--第一次命中時間
    [LAST_HIT_DATE]        NVARCHAR (50)  NULL,			--最後一次命中時間
    [LayerObject]          NVARCHAR (50)  NULL,			--層級物件
    [VDOM]                 NVARCHAR (100) NULL,			--虛擬防火牆
    [HAS_EXPIRE_DATE]      BIT            NULL,			--是否有到期日
    [EXPIRE_DATE]          NCHAR (10)     NULL,			--到期日
    [COMMENT]              NVARCHAR (500) NULL,			--備註

### FW_HW_INFO 實體防火牆資訊資料表

	[BRAND] NVARCHAR(100) NOT NULL ,		--廠牌(PK)
    [MODEL] NVARCHAR(100) NOT NULL,			--型號(PK)
    [FW_VERSION] NVARCHAR(100) NOT NULL,	--韌體版本(PK)
    [DEVICE_NAME] NVARCHAR(100) NOT NULL,	--防火牆名稱(PK)
    [DEVICE_IP] NVARCHAR(100) NOT NULL,		--防火牆IP(必填)
    [DESCRIPTION] NVARCHAR(1000) NULL,		--描述說明(選填)

### FW_API_INFO 防火牆API資訊資料表
	
	[BRAND] NVARCHAR(100) NOT NULL,			--廠牌(PK)
    [MODEL] NVARCHAR(100) NOT NULL,			--型號(PK)
    [FW_VERSION] NVARCHAR(100) NOT NULL,	--韌體版本(PK)
    [SEQUENCE] INT NOT NULL IDENTITY,		--序號(PK,自動增量)
    [API_NAME] NVARCHAR(100) NOT NULL,		--API名稱
    [API_URL] NVARCHAR(2000) NOT NULL,		--API對應的實體位址

### IP_GROUP_MAIN IP群組主表
	
	[IP_GROUP_ID] INT NOT NULL PRIMARY KEY IDENTITY,	--IP群組ID(PK)
    [NAME] NVARCHAR(100) NOT NULL						--群組名稱

### IP_GROUP_DETAILS IP群組明細表

	[IP_GROUP_DETAIL_ID] INT NOT NULL PRIMARY KEY IDENTITY, --IP群組明細ID(PK)
    [IP_GROUP_MAIN_ID] INT NULL,                            --IP群組主表ID(FK)
    [IPTYPE] TINYINT NULL,                                  --IP類型(1:單一IP,2:IP範圍,3:子網)
    [IPSTART] NVARCHAR(50) NULL,                            -- 起始IP
    [IPEND] NVARCHAR(50) NULL,                              -- 結束IP
    [IPSUBNET] NVARCHAR(50) NULL                            -- 子網

### SERVICE_GROUP_MAIN 服務群組主表
    [SERVICE_GROUP_ID] INT NOT NULL PRIMARY KEY IDENTITY,	--服務群組ID(PK)
    [NAME] NVARCHAR(100) NOT NULL							--群組名稱

### SERVICE_GROUP_DETAILS 服務群組明細表
    [SERVICE_GROUP_DETAIL_ID] INT NOT NULL PRIMARY KEY IDENTITY,    --服務群組明細ID(PK)
    [SERVICE_GROUP_ID] INT NULL,                                    --服務群組主表ID(FK)
    [PROTOCOL] NVARCHAR(50) NULL,                                   --協定
    [PORT_S] NVARCHAR(50) NULL,                                     --起始埠
    [PORT_E] NVARCHAR(50) NULL                                      --結束埠

## 介面說明 / Interface Description

### IFIREWALL_RULE_SET 介面說明(防火牆規則資料介面)
#### 屬性說明
##### DEVICE_NAME 防火牆名稱
##### RULENO 規則編號
##### UUID 規則UUID
##### SAMEKEY 相同鍵值
##### INCOMING_INTERFACE 來源接口
##### OUTGOING_INTERFACE 目的接口
##### SOURCE_GROUP 來源群組
##### SOURCE_IP_START 來源IP起始
##### SOURCE_IP_END 來源IP結束
##### SOURCE_FQDN 來源FQDN
##### SOURCE_SUBNET 來源子網
##### DESTINATION_GROUP 目的群組
##### DESTINATION_IP_START 目的IP起始
##### DESTINATION_IP_END 目的IP結束
##### DESTINATION_FQDN 目的FQDN
##### DESTINATION_SUBNET1 目的子網
##### PROTOCAL 協定
##### PORT_S 起始埠
##### PORT_E 結束埠
##### SERVICE_GROUP 服務群組
##### ACTION 行為
##### ENABLED 是否啟用
##### DATA_DT 資料日期
##### HIT 命中次數
##### FIRST_HIT_DATE 第一次命中時間
##### LAST_HIT_DATE 最後一次命中時間
##### LayerObject 層級物件
##### VDOM 虛擬防火牆
##### HAS_EXPIRE_DATE 是否有到期日
##### EXPIRE_DATE 到期日
##### COMMENT 備註

### IIPGroupInformation 介面說明(IP群組資料介面)
#### 屬性說明
##### Name 群組名稱
##### IpType IP類型(1:單一IP,2:IP範圍,3:子網)
##### IpStart 起始IP
##### IpEnd 結束IP
##### IpSubnet 子網

### IServiceInformation 介面說明(服務群組資料介面)
#### 屬性說明
##### Name 群組名稱
##### Protocol 協定
##### PortS 起始埠
##### PortE 結束埠

### IFirewallDeviceInfo 介面說明(防火牆管理主機資料介面)
#### 屬性說明
##### Brand 防火牆廠牌
##### Model 防火牆型號
##### FWVersion 防火牆韌體版本
##### DeviceName 防火牆名稱
##### DeviceIP 防火牆IP
##### Description 描述說明

### IApiUserInfo 介面說明(API使用者資訊資料介面)
#### 屬性說明
##### UserName 使用者名稱
##### Password 密碼
##### AuthType 認證類型
##### AdditionalInfo 其他資訊
##### Token 認證Token(APIKey)

### IFirewallAPI 介面說明(防火牆API資訊資料介面)
#### 屬性說明
##### IFirewallDeviceInfo 防火牆管理主機資訊
##### APIName API名稱
##### APIURL API對應的實體位址

### IFirewallAPIClient 介面方法說明
#### GetFirewallRules 取得防火牆規則方法
##### 方法參數: 無
##### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
#### GetClientInfo 取得防火牆API客戶端資訊方法
##### 方法參數: IFirewallDeviceInfo device 防火牆名稱
##### 回傳值: IFirewallAPIClient 防火牆API客戶端資訊

### ICMDBSource 介面方法說明
#### GetFirewallList 取得防火牆管理主機清單方法
##### 方法參數: 無
##### 回傳值: List&lt;IFirewallDeviceInfo&gt; 防火牆管理主機清單
#### GetAPIUser 取得API使用者資訊方法
##### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
##### 回傳值: IApiUserInfo API使用者資訊
#### DoFirewallRules 取得防火牆規則方法
##### 方法參數: IFirewallDeviceInfo device 防火牆名稱
##### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
##### 實作說明: 根據防火牆廠牌選擇對應的API客戶端實作(示範版本或實際版本)
#### GetAPIInformation 取得防火牆API資訊方法
##### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
##### 方法參數: APINames APIName API名稱列舉
##### 回傳值: IFirewallAPI 防火牆API資訊
#### GetIPGroupInformation 取得IP群組資訊方法
##### 方法參數: string groupName 群組名稱(選填, 預設為空字串)
##### 回傳值: List&lt;IIPGroupInformation&gt; IP群組資訊清單
#### GetServiceGroupInformation 取得服務群組資訊方法
##### 方法參數: string groupName 群組名稱(選填, 預設為空字串)
##### 回傳值: List&lt;IServiceInformation&gt; 服務群組資訊清單
#### SaveFirewallRules 儲存防火牆規則方法
##### 方法參數: List&lt;IFIREWALL_RULE_SET&gt; rules 防火牆規則清單
##### 回傳值: int 儲存筆數
#### GetDatabaseConnectionString 取得資料庫連線字串方法
##### 方法參數: 無
##### 回傳值: string 資料庫連線字串
#### GetCMDBConnectionString 取得CMDB系統連線字串方法
##### 方法參數: 無
##### 回傳值: string CMDB系統連線字串
#### GetAPITimeout 取得API連線逾時時間方法
##### 方法參數: 無
##### 回傳值: int API連線逾時時間(毫秒)
#### GetMaxRetryCount 取得API最大重試次數方法
##### 方法參數: 無
##### 回傳值: int API最大重試次數
#### GetRetryDelayMilliseconds 取得API重試延遲時間方法
##### 方法參數: 無
##### 回傳值: int API重試延遲時間(毫秒)
#### GetUseProxy 取得是否使用代理伺服器方法
##### 方法參數: 無
##### 回傳值: bool 是否使用代理伺服器
#### GetProxyAddress 取得代理伺服器位址方法
##### 方法參數: 無
##### 回傳值: string 代理伺服器位址
#### GetProxyPort 取得代理伺服器連接埠方法
##### 方法參數: 無
##### 回傳值: int 代理伺服器連接埠
#### GetProxyUserName 取得代理伺服器使用者名稱方法
##### 方法參數: 無
##### 回傳值: string 代理伺服器使用者名稱
#### GetProxyPassword 取得代理伺服器密碼方法
##### 方法參數: 無
##### 回傳值: string 代理伺服器密碼
#### GetLogFilePath 取得日誌檔案路徑方法
##### 方法參數: 無
##### 回傳值: string 日誌檔案路徑
#### GetLogLevel 取得日誌等級方法
##### 方法參數: 無
##### 回傳值: string 日誌等級
#### GetEnableConsoleLog 取得是否啟用主控台日誌方法
##### 方法參數: 無
##### 回傳值: bool 是否啟用主控台日誌
#### GetEnableFileLog 取得是否啟用檔案日誌方法
##### 方法參數: 無
##### 回傳值: bool 是否啟用檔案日誌
#### SendNotification 送出通知方法
##### 方法參數: string subject 主旨
##### 方法參數: string message 訊息內容
##### 回傳值: void 無回傳值

## 主要元件說明 / Main Components

### Program.cs 主程式說明
#### Main 主程式入口方法
##### 方法參數: string[] args 主程式參數
##### 回傳值: void 無回傳值
#### ConfigureServices 設定服務方法
##### 方法參數: 無
##### 回傳值: ServiceProvider 服務提供者
#### RunFirewallRuleExport 執行防火牆規則匯出方法
##### 方法參數: ICMDBSource cmdbSource CMDB資料來源
##### 回傳值: Task 執行結果

### Logger 日誌記錄說明
#### LogInfo 記錄資訊日誌方法
##### 方法參數: string message 訊息內容
##### 回傳值: void 無回傳值
#### LogWarning 記錄警告日誌方法
##### 方法參數: string message 訊息內容
##### 回傳值: void 無回傳值
#### LogError 記錄錯誤日誌方法
##### 方法參數: string message 訊息內容
##### 方法參數: Exception ex 例外狀況
##### 回傳值: void 無回傳值
#### LogDebug 記錄除錯日誌方法
##### 方法參數: string message 訊息內容
##### 回傳值: void 無回傳值
#### LogTrace 記錄追蹤日誌方法
##### 方法參數: string message 訊息內容
##### 回傳值: void 無回傳值

### FirewallRuleExporter 防火牆規則匯出說明
#### ExportFirewallRules 匯出防火牆規則方法
##### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
##### 方法參數: ICMDBSource cmdbSource CMDB資料來源
##### 回傳值: Task&lt;int&gt; 匯出筆數

## API 客戶端實作说明 / API Client Implementations

### CheckPoint 防火牆 API 客戶端
本專案提供兩種 CheckPoint API 客戶端實作:

#### 1. CheckPointFirewallAPIClient (示範版本)
##### 檔案位置: Implementation/CheckPointFirewallAPIClient.cs
##### 特色:
###### 使用模擬資料進行測試
###### 適用於開發和測試環境
###### 不需要實際的防火牆連線
##### 使用方式:
```csharp
client = new CheckPointFirewallAPIClient(device, this);
```

#### 2. CheckPointRealAPIClient (實際版本) ⭐ 生產環境建議
##### 檔案位置: Implementation/CheckPointRealAPIClient.cs
##### 文件位置: Documentation/CheckPoint_API_Implementation_Guide.md
##### 特色:
###### 真實 API 呼叫
###### 支援 API Key/Token 認證 (建議) 和帳號密碼認證
###### 自動 Session 管理
###### 完整的錯誤處理和重試機制
###### 支援 SSL 憑證處理
###### 支援 Proxy 設定
###### 智慧群組展開 (優先使用 CMDB 群組定義)
###### 遞迴展開巢狀群組
###### 取得 Policy Packages
###### 分頁查詢支援
###### 取得規則命中統計
##### 支援版本:
###### CheckPoint R80.x 以上
###### CheckPoint R81.x
##### 認證設定:
###### API Key 認證 (建議): 在 GetAPIUser 方法中設定 Token
###### 帳號密碼認證: 設定 UserName 和 Password
##### 使用方式:
```csharp
client = new CheckPointRealAPIClient(device, this);
```
##### 詳細說明請參考: Documentation/CheckPoint_API_Implementation_Guide.md

### FortiGate 防火牆 API 客戶端
本專案提供兩種 FortiGate API 客戶端實作:

#### 1. FortiGateFirewallAPIClient (示範版本)
##### 檔案位置: Implementation/FortiGateFirewallAPIClient.cs
##### 特色:
###### 使用模擬資料進行測試
###### 適用於開發和測試環境
###### 不需要實際的防火牆連線
##### 使用方式:
```csharp
client = new FortiGateFirewallAPIClient(device, this);
```

#### 2. FortiGateRealAPIClient (實際版本) ⭐ 生產環境建議
##### 檔案位置: Implementation/FortiGateRealAPIClient.cs
##### 文件位置: Documentation/FortiGate_API_Implementation_Guide.md
##### 特色:
###### 真實 API 呼叫
###### 僅支援 API Token 認證 (FortiGate 唯一認證方式)
###### 自動處理 VDOM (Virtual Domain)
###### 完整的錯誤處理和重試機制
###### 支援 SSL 憑證處理
###### 支援 Proxy 設定
###### 智慧群組展開 (優先使用 CMDB 群組定義)
###### 遞迴展開巢狀群組
###### 支援多 VDOM 環境
###### 支援地理位置物件
###### 自動子網路遮罩轉換 (轉為 CIDR 格式)
##### 支援版本:
###### FortiGate 6.x 以上
###### FortiGate 7.x
##### 認證設定:
###### API Token 認證 (唯一方式): 在 GetAPIUser 方法中設定 Token
##### 使用方式:
```csharp
client = new FortiGateRealAPIClient(device, this);
```
##### 詳細說明請參考: Documentation/FortiGate_API_Implementation_Guide.md

### 實作邏輯說明
#### 透過防火牆API取得防火牆規則(包含來源/目的IP群組及服務群組)
##### 取得防火牆規則邏輯說明
###### CheckPoint: 官方文件 https://sc1.checkpoint.com/documents/latest/APIs/
###### FortiGate: 官方文件 https://docs.fortinet.com/document/fortigate/7.0.0/rest-api-reference/
##### 需要從CMDB系統取得API使用者資訊及API位址
##### 透過防火牆API取得IP群組及服務群組資訊
##### 透過防火牆API取得防火牆規則清單
##### 透過CMDB取得IP群組及服務群組明細資訊並進行以下邏輯比對
###### 來源IP群組比對
####### 若防火牆規則來源IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 目的IP群組比對
####### 若防火牆規則目的IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 服務群組比對
####### 若防火牆規則服務群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
#### 錯誤處理說明
##### 透過API回傳的錯誤訊息進行例外狀況拋出及記錄日誌,以利後續追蹤及除錯並執行SendNotification送出通知給相關人員(例如:系統管理員)

## APINames 列舉說明(API名稱列舉)
### 實作以CheckPoint API的API名稱為主,其他廠牌可依此命名規則進行擴充
#### 命名規則: 動作_物件名稱
#### 例如: Get_FirewallRules 代表取得防火牆規則的API名稱
### 列舉值說明
#### Get_FirewallRules 取得防火牆規則
#### Get_IPGroupInformation 取得IP群組資訊
#### Get_ServiceGroupInformation 取得服務群組資訊
#### Update_Session 更新Session
#### Login_Session 登入Session
#### Logout_Session 登出Session
#### Refresh_Token 更新Token
#### Create_Token 建立Token
#### Delete_Token 刪除Token
#### Renew_Token 續約Token
#### Reset_Token 重設Token
#### Revoke_Token 撤銷Token
#### Validate_Token 驗證Token
#### Get_FirewallPolicyHits 取得防火牆規則命中次數
#### Get_FirewallLogs 取得防火牆日誌
#### Get_FirewallInterfaces 取得防火牆介面
#### Get_FirewallAddressObjects 取得防火牆地址物件
#### Get_FirewallServiceObjects 取得防火牆服務物件
#### Get_FirewallNATRules 取得防火牆NAT規則
#### Get_FirewallVPNRules 取得防火牆VPN規則
#### Get_FirewallRoutingRules 取得防火牆路由規則
#### Get_FirewallUserObjects 取得防火牆使用者物件
#### Get_FirewallGroupObjects 取得防火牆群組物件
#### Get_FirewallPolicyPackages 取得防火牆政策套件
#### Get_FirewallZones 取得防火牆區域
#### Get_FirewallVirtualServers 取得防火牆虛擬伺服器
#### Get_FirewallVirtualRouters 取得防火牆虛擬路由器
#### Get_FirewallHighAvailability 取得防火牆高可用性
#### Get_FirewallSystemInfo 取得防火牆系統資訊
#### Get_FirewallFirmwareInfo 取得防火牆韌體資訊
#### Get_FirewallLicenseInfo 取得防火牆授權資訊
#### Get_FirewallPerformanceStats 取得防火牆效能統計
#### Get_FirewallThreatLogs 取得防火牆威脅日誌
#### Get_FirewallURLLogs 取得防火牆URL日誌
#### Get_FirewallApplicationLogs 取得防火牆應用程式日誌
#### Get_FirewallBandwidthLogs 取得防火牆頻寬日誌
#### Get_FirewallConnectionLogs 取得防火牆連線日誌
#### Get_FirewallEventLogs 取得防火牆事件日誌
#### Get_FirewallAuditLogs 取得防火牆稽核日誌
#### Get_FirewallConfigBackup 取得防火牆設定備份
#### Get_FirewallConfigRestore 取得防火牆設定還原
#### Get_FirewallFirmwareUpgrade 取得防火牆韌體升級
#### Get_FirewallSystemReboot 取得防火牆系統重啟
#### Get_FirewallFactoryReset 取得防火牆恢復出廠設定
#### Get_FirewallDiagnostics 取得防火牆診斷
#### Get_FirewallSupportInfo 取得防火牆支援資訊
#### Get_FirewallHealthCheck 取得防火牆健康檢查
#### Get_FirewallBackupSchedules 取得防火牆備份排程
#### Get_FirewallAlertSettings 取得防火牆警示設定
#### Get_FirewallUserActivity 取得防火牆使用者活動
#### Get_FirewallSessionInfo 取得防火牆連線資訊
#### Get_FirewallVPNInfo 取得防火牆VPN資訊
#### Get_FirewallNATInfo 取得防火牆NAT資訊
#### Get_FirewallRoutingInfo 取得防火牆路由資訊
#### Get_FirewallInterfaceInfo 取得防火牆介面資訊
#### Get_FirewallZoneInfo 取得防火牆區域資訊
#### Get_FirewallVirtualServerInfo 取得防火牆虛擬伺服器資訊
#### Get_FirewallVirtualRouterInfo 取得防火牆虛擬路由器資訊
#### Get_FirewallHighAvailabilityInfo 取得防火牆高可用性資訊

# 使用說明

## 快速開始

### 1. 建立資料庫及資料表
#### 執行資料庫建立腳本: Database/CreateDatabase.sql
#### 插入測試資料 (選用): Database/InsertTestData.sql
#### 查詢測試資料 (選用): Database/QueryTestData.sql

### 2. 設定連線字串
#### 編輯 App.config 設定資料庫連線字串
```xml
<connectionStrings>
  <add name="DatabaseConnection" 
       connectionString="Data Source=.;Initial Catalog=FirewallDB;Integrated Security=True" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### 3. 選擇 API 客戶端實作
#### 在 DemoCMDBSource.cs 的 DoFirewallRules 方法中選擇使用:
##### 示範版本 (預設): 適用於開發/測試環境
```csharp
// CheckPoint
client = new CheckPointFirewallAPIClient(device, this);
// FortiGate
client = new FortiGateFirewallAPIClient(device, this);
```
##### 實際版本: 適用於生產環境
```csharp
// CheckPoint
client = new CheckPointRealAPIClient(device, this);
// FortiGate
client = new FortiGateRealAPIClient(device, this);
```
### 4. 設定 API 認證資訊
#### 在 DemoCMDBSource.cs 的 GetAPIUser 方法中設定:
##### CheckPoint: 支援 API Key (建議) 或帳號密碼
```csharp
return new ApiUserInfo
{
    AuthType = "Token",
    Token = "your-checkpoint-api-key-here"
};
```
##### FortiGate: 僅支援 API Token
```csharp
return new ApiUserInfo
{
    AuthType = "Token",
    Token = "your-fortigate-api-token-here"
};
```

### 5. 執行主控台應用程式
#### 執行 FirewallRuleExportDemo.exe 即可開始匯出防火牆規則至資料庫中

### 6. 檢查結果
#### 檢查資料庫中的 FIREWALL_RULE_SET 資料表以確認防火牆規則是否正確匯出
#### 檢查日誌檔案 (預設: Logs\FirewallExport.log) 以確認執行狀況

## 進階設定

### API 設定
```xml
<appSettings>
  <!-- API 連線逾時時間 (毫秒) -->
  <add key="APITimeout" value="60000" />
  
  <!-- API 最大重試次數 -->
  <add key="MaxRetryCount" value="3" />
  
  <!-- API 重試延遲時間 (毫秒) -->
  <add key="RetryDelayMilliseconds" value="2000" />
</appSettings>
```

### Proxy 設定
```xml
<appSettings>
  <!-- 是否使用 Proxy -->
  <add key="UseProxy" value="false" />
  <add key="ProxyAddress" value="proxy.company.com" />
  <add key="ProxyPort" value="8080" />
  <add key="ProxyUserName" value="" />
  <add key="ProxyPassword" value="" />
</appSettings>
```

### 日誌設定
```xml
<appSettings>
  <!-- 日誌檔案路徑 -->
  <add key="LogFilePath" value="Logs\FirewallExport.log" />
  
  <!-- 日誌等級: Trace, Debug, Info, Warning, Error -->
  <add key="LogLevel" value="Info" />
  
  <!-- 是否啟用主控台日誌 -->
  <add key="EnableConsoleLog" value="true" />
  
  <!-- 是否啟用檔案日誌 -->
  <add key="EnableFileLog" value="true" />
</appSettings>
```

## 擴充自訂實作

### 實作自訂 CMDB 資料來源
#### 建立類別實作 ICMDBSource 介面
```csharp
public class CustomCMDBSource : ICMDBSource
{
    // 實作所有介面方法
}
```

### 實作自訂防火牆 API 客戶端
#### 建立類別實作 IFirewallAPIClient 介面
```csharp
public class CustomFirewallAPIClient : IFirewallAPIClient
{
    // 實作所有介面方法
}
```

### 註冊自訂實作
#### 在 Program.cs 的 ConfigureServices 方法中註冊
```csharp
private static ServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    services.AddSingleton<ICMDBSource, CustomCMDBSource>();
    return services.BuildServiceProvider();
}
```

# 注意事項

## 安全性
### 1. 請確保您的防火牆API有足夠的權限以取得防火牆規則及相關資訊
### 2. 請妥善保管 API Token/Key，不要硬編碼在程式中
### 3. 建議使用 API Token/Key 認證而非帳號密碼
### 4. 生產環境請使用有效的 SSL 憑證
### 5. 限制 API 使用者權限為唯讀 (Read-Only)

## 相容性
### 1. 請確保您的CMDB系統有足夠的權限以取得API使用者資訊及API位址
### 2. 請根據您的防火牆廠牌及型號選擇適當的IFirewallAPIClient實作
### 3. 請根據您的CMDB系統選擇適當的ICMDBSource實作
### 4. CheckPoint 支援 R80.x 以上版本
### 5. FortiGate 支援 6.x 以上版本

## 效能
### 1. 大量規則可能需要較長的處理時間
### 2. 建議在非尖峰時段執行
### 3. 可調整 API 超時時間和重試設定
### 4. 群組展開會增加 API 呼叫次數，影響處理時間

# 文件參考

## 專案文件
### PROJECT_SUMMARY.md - 專案摘要
### INSTALLATION.md - 安裝指南
### README.md - 專案說明 (英文版)
### README_CHT.md - 專案說明 (中文版)

## API 實作指南
### Documentation/CheckPoint_API_Implementation_Guide.md - CheckPoint API 實作指南
### Documentation/FortiGate_API_Implementation_Guide.md - FortiGate API 實作指南

## 資料庫腳本
### Database/CreateDatabase.sql - 資料庫建立腳本
### Database/InsertTestData.sql - 測試資料插入腳本
### Database/QueryTestData.sql - 測試資料查詢腳本

## 官方文件
### CheckPoint: https://sc1.checkpoint.com/documents/latest/APIs/
### FortiGate: https://docs.fortinet.com/document/fortigate/7.0.0/rest-api-reference/

# 聯絡資訊

## 如有任何問題或建議,請聯絡:
### 姓名: 黃建豪 (Edward Huang)
### 電子郵件: edward.huang@kli.com.tw
### 公司: 寬聯資訊股份有限公司

# 版本歷程

## v1.1.0 (2026-01-31)
### 新增功能:
#### ✅ 新增 CheckPoint 實際 API 呼叫版本 (CheckPointRealAPIClient)
#### ✅ 新增 FortiGate 實際 API 呼叫版本 (FortiGateRealAPIClient)
#### ✅ 完整的 API 實作指南文件
#### ✅ 測試資料 SQL 腳本
#### ✅ 支援 API Token/Key 認證
#### ✅ 支援 Proxy 設定
#### ✅ 重試機制
#### ✅ SSL 憑證處理
#### ✅ 詳細日誌記錄
#### ✅ 智慧群組展開
#### ✅ VDOM 支援 (FortiGate)
#### ✅ Policy Package 支援 (CheckPoint)

## v1.0.0 (2026-01-30)
### 初始版本:
#### ✅ 基礎架構設計
#### ✅ 介面定義
#### ✅ 示範版本實作
#### ✅ 資料庫結構設計
#### ✅ 基本文件

# 授權資訊
本專案為示範專案 (DEMO)，僅供學習和參考使用。