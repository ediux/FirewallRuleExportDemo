using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Models;

namespace FirewallRuleExportDemo.Implementation
{
    /// <summary>
    /// CMDB 資料來源示範實作
    /// Demo CMDB Source Implementation
    /// 
    /// 此類別實作 ICMDBSource 介面，提供防火牆規則匯出所需的各項功能
    /// This class implements ICMDBSource interface, providing various functions required for firewall rule export
    /// </summary>
    public class DemoCMDBSource : ICMDBSource
    {
        private readonly string _databaseConnectionString;
        private readonly string _cmdbConnectionString;

        public DemoCMDBSource()
        {
            _databaseConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DatabaseConnection"]?.ConnectionString 
                ?? "Data Source=.;Initial Catalog=FirewallDB;Integrated Security=True";
            _cmdbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["CMDBConnection"]?.ConnectionString 
                ?? "Data Source=.;Initial Catalog=CMDB;Integrated Security=True";
        }

        /// <summary>
        /// 取得防火牆管理主機清單
        /// Get firewall device list
        /// </summary>
        public List<IFirewallDeviceInfo> GetFirewallList()
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                var sql = "SELECT BRAND, MODEL, FW_VERSION as FWVersion, DEVICE_NAME as DeviceName, DEVICE_IP as DeviceIP, DESCRIPTION as Description FROM FW_HW_INFO";
                var devices = conn.Query<FirewallDeviceInfo>(sql).ToList();
                return devices.Cast<IFirewallDeviceInfo>().ToList();
            }
        }

        /// <summary>
        /// 取得 API 使用者資訊
        /// Get API user information
        /// </summary>
        public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
        {
            // 根據不同廠牌回傳對應的認證資訊
            // Return corresponding authentication information based on different brands
            
            if (device.Brand.ToUpper() == "CHECKPOINT")
            {
                // CheckPoint 支援 API Key 或帳號密碼認證
                // CheckPoint supports API Key or username/password authentication
                return new ApiUserInfo
                {
                    // 選項 1: 使用 API Key (建議)
                    // Option 1: Use API Key (Recommended)
                    AuthType = "Token",
                    Token = "your-checkpoint-api-key-here",
                    
                    // 選項 2: 使用帳號密碼
                    // Option 2: Use username/password
                    // UserName = "admin",
                    // Password = "password",
                    // AuthType = "Password",
                    
                    AdditionalInfo = ""
                };
            }
            else if (device.Brand.ToUpper() == "FORTIGATE")
            {
                // FortiGate 只支援 API Token 認證
                // FortiGate only supports API Token authentication
                return new ApiUserInfo
                {
                    AuthType = "Token",
                    Token = "your-fortigate-api-token-here",
                    AdditionalInfo = ""
                };
            }
            else
            {
                // 預設回傳示範用的認證資訊
                // Return demo authentication information by default
                return new ApiUserInfo
                {
                    UserName = "admin",
                    Password = "password",
                    AuthType = "Token",
                    Token = "demo-api-token-12345",
                    AdditionalInfo = ""
                };
            }
        }

        /// <summary>
        /// 取得防火牆規則
        /// Get firewall rules
        /// 
        /// 根據防火牆廠牌選擇對應的 API 客戶端實作
        /// Select corresponding API client implementation based on firewall brand
        /// </summary>
        public async Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device)
        {
            IFirewallAPIClient client = null;

            if (device.Brand.ToUpper() == "CHECKPOINT")
            {
                // CheckPoint 防火牆
                // CheckPoint Firewall
                
                // 選項 1: 使用實際 API 呼叫版本 (生產環境建議)
                // Option 1: Use real API client (Recommended for production)
                // client = new CheckPointRealAPIClient(device, this);
                
                // 選項 2: 使用示範版本 (測試/開發環境)
                // Option 2: Use demo version (For testing/development)
                client = new CheckPointFirewallAPIClient(device, this);
            }
            else if (device.Brand.ToUpper() == "FORTIGATE")
            {
                // FortiGate 防火牆
                // FortiGate Firewall
                
                // 選項 1: 使用實際 API 呼叫版本 (生產環境建議)
                // Option 1: Use real API client (Recommended for production)
                // client = new FortiGateRealAPIClient(device, this);
                
                // 選項 2: 使用示範版本 (測試/開發環境)
                // Option 2: Use demo version (For testing/development)
                client = new FortiGateFirewallAPIClient(device, this);
            }
            else
            {
                throw new NotSupportedException($"不支援的防火牆廠牌: {device.Brand}");
            }

            return await client.GetFirewallRules();
        }

        /// <summary>
        /// 取得防火牆 API 資訊
        /// Get firewall API information
        /// </summary>
        public IFirewallAPI GetAPIInformation(IFirewallDeviceInfo device, APINames apiName)
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                var sql = @"SELECT TOP 1 API_NAME as APIName, API_URL as APIURL 
                           FROM FW_API_INFO 
                           WHERE BRAND = @Brand AND MODEL = @Model AND FW_VERSION = @FWVersion AND API_NAME = @APIName";
                
                var apiInfo = conn.QueryFirstOrDefault<FirewallAPI>(sql, new
                {
                    Brand = device.Brand,
                    Model = device.Model,
                    FWVersion = device.FWVersion,
                    APIName = apiName.ToString()
                });

                if (apiInfo != null)
                {
                    apiInfo.FirewallDeviceInfo = device;
                    return apiInfo;
                }

                // 如果資料庫中沒有設定，回傳預設 URL
                // Return default URL if not configured in database
                return new FirewallAPI
                {
                    FirewallDeviceInfo = device,
                    APIName = apiName.ToString(),
                    APIURL = $"https://{device.DeviceIP}/api/{apiName}"
                };
            }
        }

        /// <summary>
        /// 取得 IP 群組資訊
        /// Get IP group information
        /// </summary>
        public List<IIPGroupInformation> GetIPGroupInformation(string groupName = "")
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                var sql = @"SELECT m.NAME as Name, d.IPTYPE as IpType, d.IPSTART as IpStart, d.IPEND as IpEnd, d.IPSUBNET as IpSubnet
                           FROM IP_GROUP_MAIN m
                           INNER JOIN IP_GROUP_DETAILS d ON m.IP_GROUP_ID = d.IP_GROUP_MAIN_ID";

                if (!string.IsNullOrEmpty(groupName))
                {
                    sql += " WHERE m.NAME = @GroupName";
                    var groups = conn.Query<IPGroupInformation>(sql, new { GroupName = groupName }).ToList();
                    return groups.Cast<IIPGroupInformation>().ToList();
                }
                else
                {
                    var groups = conn.Query<IPGroupInformation>(sql).ToList();
                    return groups.Cast<IIPGroupInformation>().ToList();
                }
            }
        }

        /// <summary>
        /// 取得服務群組資訊
        /// Get service group information
        /// </summary>
        public List<IServiceInformation> GetServiceGroupInformation(string groupName = "")
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                var sql = @"SELECT m.NAME as Name, d.PROTOCOL as Protocol, d.PORT_S as PortS, d.PORT_E as PortE
                           FROM SERVICE_GROUP_MAIN m
                           INNER JOIN SERVICE_GROUP_DETAILS d ON m.SERVICE_GROUP_ID = d.SERVICE_GROUP_ID";

                if (!string.IsNullOrEmpty(groupName))
                {
                    sql += " WHERE m.NAME = @GroupName";
                    var services = conn.Query<ServiceInformation>(sql, new { GroupName = groupName }).ToList();
                    return services.Cast<IServiceInformation>().ToList();
                }
                else
                {
                    var services = conn.Query<ServiceInformation>(sql).ToList();
                    return services.Cast<IServiceInformation>().ToList();
                }
            }
        }

        /// <summary>
        /// 儲存防火牆規則
        /// Save firewall rules
        /// </summary>
        public async Task<int> SaveFirewallRules(List<IFIREWALL_RULE_SET> rules)
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                await conn.OpenAsync();

                // 刪除該防火牆的舊規則
                // Delete old rules for this firewall
                var deleteSql = "DELETE FROM FIREWALL_RULE_SET WHERE DEVICE_NAME = @DeviceName";
                await conn.ExecuteAsync(deleteSql, new { DeviceName = rules.First().DEVICE_NAME });

                // 插入新規則
                // Insert new rules
                var insertSql = @"INSERT INTO FIREWALL_RULE_SET 
                    (DEVICE_NAME, RULENO, UUID, SAMEKEY, INCOMING_INTERFACE, OUTGOING_INTERFACE, 
                     SOURCE_GROUP, SOURCE_IP_START, SOURCE_IP_END, SOURCE_FQDN, SOURCE_SUBNET,
                     DESTINATION_GROUP, DESTINATION_IP_START, DESTINATION_IP_END, DESTINATION_FQDN, DESTINATION_SUBNET1,
                     PROTOCAL, PORT_S, PORT_E, SERVICE_GROUP, ACTION, ENABLED, DATA_DT, HIT, 
                     FIRST_HIT_DATE, LAST_HIT_DATE, LayerObject, VDOM, HAS_EXPIRE_DATE, EXPIRE_DATE, COMMENT)
                    VALUES 
                    (@DEVICE_NAME, @RULENO, @UUID, @SAMEKEY, @INCOMING_INTERFACE, @OUTGOING_INTERFACE,
                     @SOURCE_GROUP, @SOURCE_IP_START, @SOURCE_IP_END, @SOURCE_FQDN, @SOURCE_SUBNET,
                     @DESTINATION_GROUP, @DESTINATION_IP_START, @DESTINATION_IP_END, @DESTINATION_FQDN, @DESTINATION_SUBNET1,
                     @PROTOCAL, @PORT_S, @PORT_E, @SERVICE_GROUP, @ACTION, @ENABLED, @DATA_DT, @HIT,
                     @FIRST_HIT_DATE, @LAST_HIT_DATE, @LayerObject, @VDOM, @HAS_EXPIRE_DATE, @EXPIRE_DATE, @COMMENT)";

                return await conn.ExecuteAsync(insertSql, rules);
            }
        }

        // 設定相關方法 / Configuration methods
        public string GetDatabaseConnectionString() => _databaseConnectionString;
        public string GetCMDBConnectionString() => _cmdbConnectionString;
        public int GetAPITimeout() => 30000;
        public int GetMaxRetryCount() => 3;
        public int GetRetryDelayMilliseconds() => 1000;
        public bool GetUseProxy() => false;
        public string GetProxyAddress() => "";
        public int GetProxyPort() => 0;
        public string GetProxyUserName() => "";
        public string GetProxyPassword() => "";
        public string GetLogFilePath() => "Logs\\FirewallExport.log";
        public string GetLogLevel() => "Info";
        public bool GetEnableConsoleLog() => true;
        public bool GetEnableFileLog() => true;

        /// <summary>
        /// 送出通知
        /// Send notification
        /// </summary>
        public void SendNotification(string subject, string message)
        {
            Console.WriteLine($"通知 - 主旨: {subject}");
            Console.WriteLine($"內容: {message}");
        }
    }
}
