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

        public List<IFirewallDeviceInfo> GetFirewallList()
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                var sql = "SELECT BRAND, MODEL, FW_VERSION as FWVersion, DEVICE_NAME as DeviceName, DEVICE_IP as DeviceIP, DESCRIPTION as Description FROM FW_HW_INFO";
                var devices = conn.Query<FirewallDeviceInfo>(sql).ToList();
                return devices.Cast<IFirewallDeviceInfo>().ToList();
            }
        }

        public IApiUserInfo GetAPIUser(IFirewallDeviceInfo device)
        {
            return new ApiUserInfo
            {
                UserName = "admin",
                Password = "password",
                AuthType = "Token",
                Token = "demo-api-token-12345",
                AdditionalInfo = ""
            };
        }

        public async Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device)
        {
            IFirewallAPIClient client = null;

            if (device.Brand.ToUpper() == "CHECKPOINT")
            {
                client = new CheckPointFirewallAPIClient(device, this);
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

                return new FirewallAPI
                {
                    FirewallDeviceInfo = device,
                    APIName = apiName.ToString(),
                    APIURL = $"https://{device.DeviceIP}/api/{apiName}"
                };
            }
        }

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

        public async Task<int> SaveFirewallRules(List<IFIREWALL_RULE_SET> rules)
        {
            using (var conn = new SqlConnection(_databaseConnectionString))
            {
                await conn.OpenAsync();

                var deleteSql = "DELETE FROM FIREWALL_RULE_SET WHERE DEVICE_NAME = @DeviceName";
                await conn.ExecuteAsync(deleteSql, new { DeviceName = rules.First().DEVICE_NAME });

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

        public void SendNotification(string subject, string message)
        {
            Console.WriteLine($"通知 - 主旨: {subject}");
            Console.WriteLine($"內容: {message}");
        }
    }
}
