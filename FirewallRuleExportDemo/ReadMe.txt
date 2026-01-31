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

## IFIREWALL_RULE_SET 介面說明(防火牆規則資料介面)
### 屬性說明
#### DEVICE_NAME 防火牆名稱
#### RULENO 規則編號
#### UUID 規則UUID
#### SAMEKEY 相同鍵值
#### INCOMING_INTERFACE 來源接口
#### OUTGOING_INTERFACE 目的接口
#### SOURCE_GROUP 來源群組
#### SOURCE_IP_START 來源IP起始
#### SOURCE_IP_END 來源IP結束
#### SOURCE_FQDN 來源FQDN
#### SOURCE_SUBNET 來源子網
#### DESTINATION_GROUP 目的群組
#### DESTINATION_IP_START 目的IP起始
#### DESTINATION_IP_END 目的IP結束
#### DESTINATION_FQDN 目的FQDN
#### DESTINATION_SUBNET1 目的子網
#### PROTOCAL 協定
#### PORT_S 起始埠
#### PORT_E 結束埠
#### SERVICE_GROUP 服務群組
#### ACTION 行為
#### ENABLED 是否啟用
#### DATA_DT 資料日期
#### HIT 命中次數
#### FIRST_HIT_DATE 第一次命中時間
#### LAST_HIT_DATE 最後一次命中時間
#### LayerObject 層級物件
#### VDOM 虛擬防火牆
#### HAS_EXPIRE_DATE 是否有到期日
#### EXPIRE_DATE 到期日
#### COMMENT 備註

## IIPGroupInformation 介面說明(IP群組資料介面)
### 屬性說明
#### Name 群組名稱
#### IpType IP類型(1:單一IP,2:IP範圍,3:子網)
#### IpStart 起始IP
#### IpEnd 結束IP
#### IpSubnet 子網

## IServiceInformation 介面說明(服務群組資料介面)
### 屬性說明
#### Name 群組名稱
#### Protocol 協定
#### PortS 起始埠
#### PortE 結束埠

## IFirewallDeviceInfo 介面說明(防火牆管理主機資料介面)
### 屬性說明
#### Brand 防火牆廠牌
#### Model 防火牆型號
#### FWVersion 防火牆韌體版本
#### DeviceName 防火牆名稱
#### DeviceIP 防火牆IP
#### Description 描述說明

## IApiUserInfo 介面說明(API使用者資訊資料介面)
### 屬性說明
#### UserName 使用者名稱
#### Password 密碼
#### AuthType 認證類型
#### AdditionalInfo 其他資訊
#### Token 認證Token(APIKey)

## IFirewallAPI 介面說明(防火牆API資訊資料介面)
### 屬性說明
#### IFirewallDeviceInfo 防火牆管理主機資訊
#### APIName API名稱
#### APIURL API對應的實體位址


## IFirewallAPIClient 介面方法說明
### GetFirewallRules 取得防火牆規則方法
#### 方法參數: 無
#### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
### GetClientInfo 取得防火牆API客戶端資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆名稱
#### 回傳值: IFirewallAPIClient 防火牆API客戶端資訊


## ICMDBSource 介面方法說明
### GetFirewallList 取得防火牆管理主機清單方法
#### 方法參數: 無
#### 回傳值: List&lt;IFirewallDeviceInfo&gt; 防火牆管理主機清單
### GetAPIUser 取得API使用者資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
#### 回傳值: List&lt;IApiUserInfo&gt; API使用者資訊清單
### DoFirewallRules 取得防火牆規則方法
#### 方法參數: IFirewallDeviceInfo device 防火牆名稱
#### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
### GetAPIUser 取得API使用者資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆名稱
#### 回傳值: IApiUserInfo API使用者資訊
### GetAPIInformation 取得防火牆API資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
#### 方法參數: APINames APIName API名稱列舉
#### 回傳值: IFirewallAPI 防火牆API資訊
### GetIPGroupInformation 取得IP群組資訊方法
#### 方法參數: string groupName 群組名稱(選填, 預設為空字串)
#### 回傳值: List&lt;IIPGroupInformation&gt; IP群組資訊清單
### GetServiceGroupInformation 取得服務群組資訊方法
#### 方法參數: string groupName 群組名稱(選填, 預設為空字串)
#### 回傳值: List&lt;IServiceInformation&gt; 服務群組資訊清單
### SaveFirewallRules 儲存防火牆規則方法
#### 方法參數: List&lt;IFIREWALL_RULE_SET&gt; rules 防火牆規則清單
#### 回傳值: int 儲存筆數
### GetDatabaseConnectionString 取得資料庫連線字串方法
#### 方法參數: 無
#### 回傳值: string 資料庫連線字串
### GetCMDBConnectionString 取得CMDB系統連線字串方法
#### 方法參數: 無
#### 回傳值: string CMDB系統連線字串
### GetAPITimeout 取得API連線逾時時間方法
#### 方法參數: 無
#### 回傳值: int API連線逾時時間(毫秒)
### GetMaxRetryCount 取得API最大重試次數方法
#### 方法參數: 無
#### 回傳值: int API最大重試次數
### GetRetryDelayMilliseconds 取得API重試延遲時間方法
#### 方法參數: 無
#### 回傳值: int API重試延遲時間(毫秒)
### GetUseProxy 取得是否使用代理伺服器方法
#### 方法參數: 無
#### 回傳值: bool 是否使用代理伺服器
### GetProxyAddress 取得代理伺服器位址方法
#### 方法參數: 無
#### 回傳值: string 代理伺服器位址
### GetProxyPort 取得代理伺服器連接埠方法
#### 方法參數: 無
#### 回傳值: int 代理伺服器連接埠
### GetProxyUserName 取得代理伺服器使用者名稱方法
#### 方法參數: 無
#### 回傳值: string 代理伺服器使用者名稱
### GetProxyPassword 取得代理伺服器密碼方法
#### 方法參數: 無
#### 回傳值: string 代理伺服器密碼
### GetLogFilePath 取得日誌檔案路徑方法
#### 方法參數: 無
#### 回傳值: string 日誌檔案路徑
### GetLogLevel 取得日誌等級方法
#### 方法參數: 無
#### 回傳值: string 日誌等級
### GetEnableConsoleLog 取得是否啟用主控台日誌方法
#### 方法參數: 無
#### 回傳值: bool 是否啟用主控台日誌
### GetEnableFileLog 取得是否啟用檔案日誌方法
#### 方法參數: 無
#### 回傳值: bool 是否啟用檔案日誌
### SendNotification 送出通知方法
#### 方法參數: string subject 主旨
#### 方法參數: string message 訊息內容
#### 回傳值: void 無回傳值


## Program.cs 主程式說明
### Main 主程式入口方法
#### 方法參數: string[] args 主程式參數
#### 回傳值: void 無回傳值
### ConfigureServices 設定服務方法
#### 方法參數: IServiceCollection services 服務集合
#### 回傳值: void 無回傳值
### RunFirewallRuleExport 執行防火牆規則匯出方法
#### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
#### 回傳值: Task 執行結果

## Logger 日誌記錄說明
### LogInfo 記錄資訊日誌方法
#### 方法參數: string message 訊息內容
#### 回傳值: void 無回傳值
### LogWarning 記錄警告日誌方法
#### 方法參數: string message 訊息內容
#### 回傳值: void 無回傳值
### LogError 記錄錯誤日誌方法
#### 方法參數: string message 訊息內容
#### 方法參數: Exception ex 例外狀況
#### 回傳值: void 無回傳值
### LogDebug 記錄除錯日誌方法
#### 方法參數: string message 訊息內容
#### 回傳值: void 無回傳值
### LogTrace 記錄追蹤日誌方法
#### 方法參數: string message 訊息內容
#### 回傳值: void 無回傳值
## FirewallRuleExporter 防火牆規則匯出說明
### ExportFirewallRules 匯出防火牆規則方法
#### 方法參數: IFirewallDeviceInfo device 防火牆裝置資訊
#### 方法參數: ICMDBSource cmdbSource CMDB資料來源
#### 回傳值: Task&lt;int&gt; 匯出筆數

## CheckPointFirewallAPIClient CheckPoint防火牆API客戶端實作說明
### 實作說明
#### 透過CheckPoint防火牆API取得防火牆規則(包含來源/目的IP群組及服務群組)
##### 取得防火牆規則邏輯說明(官方文件:https://sc1.checkpoint.com/documents/latest/APIs/?#web/login~v1.9.1%20)
##### 需要從CMDB系統取得API使用者資訊及API位址
##### 透過CheckPoint防火牆API取得IP群組及服務群組資訊
##### 透過CheckPoint防火牆API取得防火牆規則清單
##### 透過CMDB取得IP群組及服務群組明細資訊並進行以下邏輯比對
###### 來源IP群組比對
####### 若防火牆規則來源IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過CheckPoint防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 目的IP群組比對
####### 若防火牆規則目的IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過CheckPoint防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 服務群組比對
####### 若防火牆規則服務群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過CheckPoint防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
#### 支援CheckPoint R80.x版本以上防火牆
#### 需使用API Token進行認證
#### 支援取得防火牆規則包含以下欄位:
##### 規則編號
##### 規則UUID
##### 來源介面
##### 目的介面
##### 來源IP群組
##### 來源IP起始
##### 來源IP結束
##### 來源FQDN
##### 來源子網
##### 目的IP群組
##### 目的IP起始
##### 目的IP結束
##### 目的FQDN
##### 目的子網
##### 協定
##### 起始埠
##### 結束埠
##### 服務群組
##### 行為
##### 是否啟用
##### 命中次數
##### 第一次命中時間
##### 最後一次命中時間
##### 層級物件
##### 到期日
### GetFirewallRules 取得防火牆規則方法
#### 方法參數: 無
#### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
### GetClientInfo 取得防火牆API客戶端資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆名稱
#### 回傳值: IFirewallAPIClient 防火牆API客戶端資訊

## FortiGateFirewallAPIClient FortiGate防火牆API客戶端實作說明
### 實作說明
#### 透過FortiGate防火牆API取得防火牆規則(包含來源/目的IP群組及服務群組)
##### 取得防火牆規則邏輯說明(官方文件:https://docs.fortinet.com/document/fortigate/7.0.0/cookbook/763437/rest-api-to-manage-firewall-policies)
##### 需要從CMDB系統取得API使用者資訊及API位址
##### 透過FortiGate防火牆API取得IP群組及服務群組資訊
##### 透過FortiGate防火牆API取得防火牆規則清單
##### 透過CMDB取得IP群組及服務群組明細資訊並進行以下邏輯比對
###### 來源IP群組比對
####### 若防火牆規則來源IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過FortiGate防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 目的IP群組比對
####### 若防火牆規則目的IP群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過FortiGate防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
###### 服務群組比對
####### 若防火牆規則服務群組名稱存在於CMDB系統,則不繼續取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中,否則透過FortiGate防火牆API取得該群組明細資訊並展開至防火牆規則資料介面(IFIREWALL_RULE_SET)中
#### 支援FortiGate 6.x版本以上防火牆
#### 需使用API Token進行認證
#### 支援取得防火牆規則包含以下欄位:
##### 規則編號
##### 規則UUID
##### 來源介面
##### 目的介面
##### 來源IP群組
##### 來源IP起始
##### 來源IP結束
##### 來源FQDN
##### 來源子網
##### 目的IP群組
##### 目的IP起始
##### 目的IP結束
##### 目的FQDN
##### 目的子網
##### 協定
##### 起始埠
##### 結束埠
##### 服務群組
##### 行為
##### 是否啟用
##### 命中次數
##### 第一次命中時間
##### 最後一次命中時間
##### 層級物件
##### 到期日
### GetFirewallRules 取得防火牆規則方法
#### 方法參數: 無
#### 回傳值: List&lt;IFIREWALL_RULE_SET&gt; 防火牆規則清單
### GetClientInfo 取得防火牆API客戶端資訊方法
#### 方法參數: IFirewallDeviceInfo device 防火牆名稱
#### 回傳值: IFirewallAPIClient 防火牆API客戶端資訊

## APINames 列舉說明(API名稱列舉)
### 實作以CheckPoint_CheckPointAPI的API名稱為主,其他廠牌可依此命名規則進行擴充
#### 命名規則: 動作_物件名稱
#### 例如: Get_FirewallRules
### 代表取得防火牆規則的API名稱
### Get_FirewallRules 代表取得防火牆規則的API名稱
### Get_IPGroupInformation 代表取得IP群組資訊的API名稱
### Get_ServiceGroupInformation 代表取得服務群組資訊的API名稱
### Update_Session 代表更新Session的API名稱
### Login_Session 代表登入Session的API名稱
### Logout_Session 代表登出Session的API名稱
### Refresh_Token 代表更新Token的API名稱
### Create_Token 代表建立Token的API名稱
### Delete_Token 代表刪除Token的API名稱
### Renew_Token 代表續約Token的API名稱
### Reset_Token 代表重設Token的API名稱
### Revoke_Token 代表撤銷Token的API名稱
### Validate_Token 代表驗證Token的API名稱
### Get_FirewallPolicyHits 代表取得防火牆規則命中次數的API名稱
### Get_FirewallLogs 代表取得防火牆日誌的API名稱
### Get_FirewallInterfaces 代表取得防火牆介面的API名稱
### Get_FirewallAddressObjects 代表取得防火牆地址物件的API名稱
### Get_FirewallServiceObjects 代表取得防火牆服務物件的API名稱
### Get_FirewallNATRules 代表取得防火牆NAT規則的API名稱
### Get_FirewallVPNRules 代表取得防火牆VPN規則的API名稱
### Get_FirewallRoutingRules 代表取得防火牆路由規則的API名稱
### Get_FirewallUserObjects 代表取得防火牆使用者物件的API名稱
### Get_FirewallGroupObjects 代表取得防火牆群組物件的API名稱
### Get_FirewallPolicyPackages 代表取得防火牆政策套件的API名稱
### Get_FirewallZones 代表取得防火牆區域的API名稱
### Get_FirewallVirtualServers 代表取得防火牆虛擬伺服器的API名稱
### Get_FirewallVirtualRouters 代表取得防火牆虛擬路由器的API名稱
### Get_FirewallHighAvailability 代表取得防火牆高可用性的API名稱
### Get_FirewallSystemInfo 代表取得防火牆系統資訊的API名稱
### Get_FirewallFirmwareInfo 代表取得防火牆韌體資訊的API名稱
### Get_FirewallLicenseInfo 代表取得防火牆授權資訊的API名稱
### Get_FirewallPerformanceStats 代表取得防火牆效能統計的API名稱
### Get_FirewallThreatLogs 代表取得防火牆威脅日誌的API名稱
### Get_FirewallURLLogs 代表取得防火牆URL日誌的API名稱
### Get_FirewallApplicationLogs 代表取得防火牆應用程式日誌的API名稱
### Get_FirewallBandwidthLogs 代表取得防火牆頻寬日誌的API名稱
### Get_FirewallConnectionLogs 代表取得防火牆連線日誌的API名稱
### Get_FirewallEventLogs 代表取得防火牆事件日誌的API名稱
### Get_FirewallAuditLogs 代表取得防火牆稽核日誌的API名稱
### Get_FirewallConfigBackup 代表取得防火牆設定備份的API名稱
### Get_FirewallConfigRestore 代表取得防火牆設定還原的API名稱
### Get_FirewallFirmwareUpgrade 代表取得防火牆韌體升級的API名稱
### Get_FirewallSystemReboot 代表取得防火牆系統重啟的API名稱
### Get_FirewallFactoryReset 代表取得防火牆恢復出廠設定的API名稱
### Get_FirewallDiagnostics 代表取得防火牆診斷的API名稱
### Get_FirewallSupportInfo 代表取得防火牆支援資訊的API名稱
### Get_FirewallHealthCheck 代表取得防火牆健康檢查的API名稱
### Get_FirewallBackupSchedules 代表取得防火牆備份排程的API名稱
### Get_FirewallAlertSettings 代表取得防火牆警示設定的API名稱
### Get_FirewallUserActivity 代表取得防火牆使用者活動的API名稱
### Get_FirewallSessionInfo 代表取得防火牆連線資訊的API名稱
### Get_FirewallVPNInfo 代表取得防火牆VPN資訊的API名稱
### Get_FirewallNATInfo 代表取得防火牆NAT資訊的API名稱
### Get_FirewallRoutingInfo 代表取得防火牆路由資訊的API名稱
### Get_FirewallInterfaceInfo 代表取得防火牆介面資訊的API名稱
### Get_FirewallZoneInfo 代表取得防火牆區域資訊的API名稱
### Get_FirewallVirtualServerInfo 代表取得防火牆虛擬伺服器資訊的API名稱
### Get_FirewallVirtualRouterInfo 代表取得防火牆虛擬路由器資訊的API名稱
### Get_FirewallHighAvailabilityInfo 代表取得防火牆高可用性資訊的API名稱


# 使用說明
## 1. 建立資料庫及資料表
### 請參考上述資料表結構說明建立資料庫及資料表
## 2. 實作ICMDBSource介面以對應您的CMDB系統
### 請參考上述ICMDBSource介面方法說明進行實作
## 3. 實作IFirewallAPIClient介面以對應您的防火牆廠牌API
### 請參考上述IFirewallAPIClient介面方法說明進行實作
## 4. 設定DI容器以注入您的ICMDBSource及IFirewallAPIClient實作
### 請參考Program.cs中的ConfigureServices方法進行設定
## 5. 執行主控台應用程式
### 執行FirewallRuleExportDemo.exe即可開始匯出防火牆規則至資料庫中
## 6. 檢查資料庫中的FIREWALL_RULE_SET資料表以確認防火牆規則是否正確匯出
# 注意事項
## 1. 請確保您的防火牆API有足夠的權限以取得防火牆規則及相關資訊
## 2. 請確保您的CMDB系統有足夠的權限以取得API使用者資訊及API位址
## 3. 請根據您的防火牆廠牌及型號選擇適當的IFirewallAPIClient實作
## 4. 請根據您的CMDB系統選擇適當的ICMDBSource實作
# 聯絡資訊
## 如有任何問題或建議,請聯絡:
### 姓名: 黃建豪
### 電子郵件: edward.huang@kli.com.tw
### 公司: 寬聯資訊股份有限公司