using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Models;
using FirewallRuleExportDemo.Utilities;
using Newtonsoft.Json.Linq;

namespace FirewallRuleExportDemo.Implementation
{
    public class CheckPointFirewallAPIClient : IFirewallAPIClient
    {
        private readonly IFirewallDeviceInfo _device;
        private readonly ICMDBSource _cmdbSource;
        private readonly HttpClient _httpClient;
        private string _sessionId;

        public CheckPointFirewallAPIClient(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
        {
            _device = device;
            _cmdbSource = cmdbSource;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(cmdbSource.GetAPITimeout())
            };
        }

        public IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device)
        {
            return new CheckPointFirewallAPIClient(device, _cmdbSource);
        }

        public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
        {
            var rules = new List<IFIREWALL_RULE_SET>();

            try
            {
                Logger.LogInfo($"[CheckPoint] 開始取得防火牆規則 - {_device.DeviceName}");

                await Login();

                var cmdbIPGroups = _cmdbSource.GetIPGroupInformation();
                var cmdbServiceGroups = _cmdbSource.GetServiceGroupInformation();

                var apiRules = await FetchRulesFromAPI();

                int sameKey = 1;
                foreach (var apiRule in apiRules)
                {
                    var expandedRules = ExpandRule(apiRule, cmdbIPGroups, cmdbServiceGroups, sameKey);
                    rules.AddRange(expandedRules);
                    sameKey += expandedRules.Count;
                }

                await Logout();

                Logger.LogInfo($"[CheckPoint] 成功取得 {rules.Count} 筆防火牆規則");
                return rules;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CheckPoint] 取得防火牆規則時發生錯誤", ex);
                throw;
            }
        }

        private async Task Login()
        {
            var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Login_Session);
            var userInfo = _cmdbSource.GetAPIUser(_device);

            var loginData = new
            {
                user = userInfo.UserName,
                password = userInfo.Password
            };

            Logger.LogDebug($"[CheckPoint] 登入 API: {apiInfo.APIURL}");
            _sessionId = "demo-session-" + Guid.NewGuid().ToString();
            Logger.LogInfo($"[CheckPoint] 登入成功 - Session ID: {_sessionId}");
        }

        private async Task<List<JObject>> FetchRulesFromAPI()
        {
            var rules = new List<JObject>();

            Logger.LogInfo($"[CheckPoint] 模擬取得防火牆規則資料...");

            var demoRule1 = new JObject
            {
                ["rule-number"] = "1",
                ["uid"] = "rule-001",
                ["name"] = "Allow HTTP",
                ["source"] = new JArray(new JObject { ["name"] = "Internal-Network", ["type"] = "group" }),
                ["destination"] = new JArray(new JObject { ["name"] = "Web-Servers", ["type"] = "group" }),
                ["service"] = new JArray(new JObject { ["name"] = "HTTP-Service", ["type"] = "service-group" }),
                ["action"] = new JObject { ["name"] = "Accept" },
                ["enabled"] = true,
                ["hits"] = new JObject { ["value"] = 1500, ["first-date"] = "2024-01-01", ["last-date"] = "2024-01-15" },
                ["layer"] = "Network",
                ["comments"] = "允許內部網路存取Web伺服器"
            };

            var demoRule2 = new JObject
            {
                ["rule-number"] = "2",
                ["uid"] = "rule-002",
                ["name"] = "Allow HTTPS",
                ["source"] = new JArray(new JObject { ["name"] = "192.168.1.100", ["type"] = "host" }),
                ["destination"] = new JArray(new JObject { ["name"] = "10.0.0.50", ["type"] = "host" }),
                ["service"] = new JArray(new JObject { ["name"] = "https", ["type"] = "service-tcp", ["port"] = "443" }),
                ["action"] = new JObject { ["name"] = "Accept" },
                ["enabled"] = true,
                ["hits"] = new JObject { ["value"] = 500 },
                ["layer"] = "Network"
            };

            rules.Add(demoRule1);
            rules.Add(demoRule2);

            return rules;
        }

        private List<IFIREWALL_RULE_SET> ExpandRule(JObject apiRule, List<IIPGroupInformation> cmdbIPGroups, 
            List<IServiceInformation> cmdbServiceGroups, int baseSameKey)
        {
            var expandedRules = new List<IFIREWALL_RULE_SET>();

            var ruleNo = apiRule["rule-number"]?.ToString();
            var uuid = apiRule["uid"]?.ToString();
            var action = apiRule["action"]?["name"]?.ToString();
            var enabled = apiRule["enabled"]?.Value<bool>() ?? true;
            var comment = apiRule["comments"]?.ToString();
            var layer = apiRule["layer"]?.ToString();

            var sources = apiRule["source"] as JArray ?? new JArray();
            var destinations = apiRule["destination"] as JArray ?? new JArray();
            var services = apiRule["service"] as JArray ?? new JArray();

            var sourceList = ExpandIPObjects(sources, cmdbIPGroups);
            var destList = ExpandIPObjects(destinations, cmdbIPGroups);
            var serviceList = ExpandServiceObjects(services, cmdbServiceGroups);

            if (sourceList.Count == 0) sourceList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
            if (destList.Count == 0) destList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
            if (serviceList.Count == 0) serviceList.Add(new { Group = "", Protocol = "Any", PortS = (int?)null, PortE = (int?)null });

            int sameKey = baseSameKey;
            foreach (var src in sourceList)
            {
                foreach (var dst in destList)
                {
                    foreach (var svc in serviceList)
                    {
                        var rule = new FirewallRuleSet
                        {
                            DEVICE_NAME = _device.DeviceName,
                            RULENO = ruleNo,
                            UUID = uuid,
                            SAMEKEY = sameKey++,
                            SOURCE_GROUP = src.Group,
                            SOURCE_IP_START = src.IPStart,
                            SOURCE_IP_END = src.IPEnd,
                            SOURCE_FQDN = src.FQDN,
                            SOURCE_SUBNET = src.Subnet,
                            DESTINATION_GROUP = dst.Group,
                            DESTINATION_IP_START = dst.IPStart,
                            DESTINATION_IP_END = dst.IPEnd,
                            DESTINATION_FQDN = dst.FQDN,
                            DESTINATION_SUBNET1 = dst.Subnet,
                            SERVICE_GROUP = svc.Group,
                            PROTOCAL = svc.Protocol,
                            PORT_S = svc.PortS,
                            PORT_E = svc.PortE,
                            ACTION = action,
                            ENABLED = enabled,
                            DATA_DT = DateTime.Now.ToString("yyyy-MM-dd"),
                            HIT = apiRule["hits"]?["value"]?.Value<int>(),
                            FIRST_HIT_DATE = apiRule["hits"]?["first-date"]?.ToString(),
                            LAST_HIT_DATE = apiRule["hits"]?["last-date"]?.ToString(),
                            LayerObject = layer,
                            COMMENT = comment
                        };

                        expandedRules.Add(rule);
                    }
                }
            }

            return expandedRules;
        }

        private List<dynamic> ExpandIPObjects(JArray ipObjects, List<IIPGroupInformation> cmdbIPGroups)
        {
            var result = new List<dynamic>();

            foreach (var obj in ipObjects)
            {
                var name = obj["name"]?.ToString();
                var type = obj["type"]?.ToString();

                if (type == "group")
                {
                    var groupInCMDB = cmdbIPGroups.Where(g => g.Name == name).ToList();
                    if (groupInCMDB.Any())
                    {
                        result.Add(new { Group = name, IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                    }
                    else
                    {
                        var groupMembers = FetchIPGroupFromAPI(name);
                        result.AddRange(groupMembers);
                    }
                }
                else if (type == "host")
                {
                    result.Add(new { Group = "", IPStart = name, IPEnd = name, FQDN = "", Subnet = "" });
                }
                else if (type == "network")
                {
                    var subnet = obj["subnet"]?.ToString() ?? name;
                    result.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = subnet });
                }
            }

            return result;
        }

        private List<dynamic> ExpandServiceObjects(JArray serviceObjects, List<IServiceInformation> cmdbServiceGroups)
        {
            var result = new List<dynamic>();

            foreach (var obj in serviceObjects)
            {
                var name = obj["name"]?.ToString();
                var type = obj["type"]?.ToString();

                if (type == "service-group")
                {
                    var groupInCMDB = cmdbServiceGroups.Where(g => g.Name == name).ToList();
                    if (groupInCMDB.Any())
                    {
                        result.Add(new { Group = name, Protocol = "", PortS = (int?)null, PortE = (int?)null });
                    }
                    else
                    {
                        var groupMembers = FetchServiceGroupFromAPI(name);
                        result.AddRange(groupMembers);
                    }
                }
                else if (type == "service-tcp" || type == "service-udp")
                {
                    var protocol = type == "service-tcp" ? "TCP" : "UDP";
                    var port = obj["port"]?.ToString();
                    int? portNum = null;
                    if (!string.IsNullOrEmpty(port))
                    {
                        int.TryParse(port, out int p);
                        portNum = p;
                    }
                    result.Add(new { Group = "", Protocol = protocol, PortS = portNum, PortE = portNum });
                }
            }

            return result;
        }

        private List<dynamic> FetchIPGroupFromAPI(string groupName)
        {
            Logger.LogDebug($"[CheckPoint] 模擬從API取得IP群組: {groupName}");
            return new List<dynamic>
            {
                new { Group = "", IPStart = "192.168.1.1", IPEnd = "192.168.1.254", FQDN = "", Subnet = "" }
            };
        }

        private List<dynamic> FetchServiceGroupFromAPI(string groupName)
        {
            Logger.LogDebug($"[CheckPoint] 模擬從API取得服務群組: {groupName}");
            return new List<dynamic>
            {
                new { Group = "", Protocol = "TCP", PortS = (int?)80, PortE = (int?)80 }
            };
        }

        private async Task Logout()
        {
            Logger.LogInfo($"[CheckPoint] 登出成功");
        }
    }
}
