using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Models;
using FirewallRuleExportDemo.Utilities;
using Newtonsoft.Json.Linq;

namespace FirewallRuleExportDemo.Implementation
{
    public class FortiGateFirewallAPIClient : IFirewallAPIClient
    {
        private readonly IFirewallDeviceInfo _device;
        private readonly ICMDBSource _cmdbSource;
        private readonly HttpClient _httpClient;

        public FortiGateFirewallAPIClient(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
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
            return new FortiGateFirewallAPIClient(device, _cmdbSource);
        }

        public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
        {
            var rules = new List<IFIREWALL_RULE_SET>();

            try
            {
                Logger.LogInfo($"[FortiGate] 開始取得防火牆規則 - {_device.DeviceName}");

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

                Logger.LogInfo($"[FortiGate] 成功取得 {rules.Count} 筆防火牆規則");
                return rules;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[FortiGate] 取得防火牆規則時發生錯誤", ex);
                throw;
            }
        }

        private async Task<List<JObject>> FetchRulesFromAPI()
        {
            var rules = new List<JObject>();

            Logger.LogInfo($"[FortiGate] 模擬取得防火牆規則資料...");

            var demoRule1 = new JObject
            {
                ["policyid"] = 1,
                ["uuid"] = "fg-rule-001",
                ["name"] = "Allow-HTTP-Traffic",
                ["srcintf"] = new JArray(new JObject { ["name"] = "port1" }),
                ["dstintf"] = new JArray(new JObject { ["name"] = "port2" }),
                ["srcaddr"] = new JArray(new JObject { ["name"] = "Internal-Subnet", ["type"] = "group" }),
                ["dstaddr"] = new JArray(new JObject { ["name"] = "External-Servers", ["type"] = "group" }),
                ["service"] = new JArray(new JObject { ["name"] = "HTTP-Services", ["type"] = "group" }),
                ["action"] = "accept",
                ["status"] = "enable",
                ["hit-count"] = 2500,
                ["first-hit"] = "2024-01-01 10:00:00",
                ["last-hit"] = "2024-01-20 15:30:00",
                ["comments"] = "允許HTTP流量"
            };

            var demoRule2 = new JObject
            {
                ["policyid"] = 2,
                ["uuid"] = "fg-rule-002",
                ["name"] = "Allow-SSH",
                ["srcintf"] = new JArray(new JObject { ["name"] = "port1" }),
                ["dstintf"] = new JArray(new JObject { ["name"] = "port3" }),
                ["srcaddr"] = new JArray(new JObject { ["name"] = "10.10.10.100", ["type"] = "ipmask", ["subnet"] = "10.10.10.100 255.255.255.255" }),
                ["dstaddr"] = new JArray(new JObject { ["name"] = "172.16.0.50", ["type"] = "ipmask", ["subnet"] = "172.16.0.50 255.255.255.255" }),
                ["service"] = new JArray(new JObject { ["name"] = "SSH", ["protocol"] = "TCP", ["tcp-portrange"] = "22" }),
                ["action"] = "accept",
                ["status"] = "enable",
                ["hit-count"] = 150
            };

            rules.Add(demoRule1);
            rules.Add(demoRule2);

            return rules;
        }

        private List<IFIREWALL_RULE_SET> ExpandRule(JObject apiRule, List<IIPGroupInformation> cmdbIPGroups,
            List<IServiceInformation> cmdbServiceGroups, int baseSameKey)
        {
            var expandedRules = new List<IFIREWALL_RULE_SET>();

            var ruleNo = apiRule["policyid"]?.ToString();
            var uuid = apiRule["uuid"]?.ToString();
            var action = apiRule["action"]?.ToString();
            var enabled = apiRule["status"]?.ToString() == "enable";
            var comment = apiRule["comments"]?.ToString();

            var srcIntf = apiRule["srcintf"]?[0]?["name"]?.ToString();
            var dstIntf = apiRule["dstintf"]?[0]?["name"]?.ToString();

            var sources = apiRule["srcaddr"] as JArray ?? new JArray();
            var destinations = apiRule["dstaddr"] as JArray ?? new JArray();
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
                            INCOMING_INTERFACE = srcIntf,
                            OUTGOING_INTERFACE = dstIntf,
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
                            HIT = apiRule["hit-count"]?.Value<int>(),
                            FIRST_HIT_DATE = apiRule["first-hit"]?.ToString(),
                            LAST_HIT_DATE = apiRule["last-hit"]?.ToString(),
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
                else if (type == "ipmask")
                {
                    var subnet = obj["subnet"]?.ToString();
                    if (!string.IsNullOrEmpty(subnet))
                    {
                        var parts = subnet.Split(' ');
                        if (parts.Length == 2 && parts[1] == "255.255.255.255")
                        {
                            result.Add(new { Group = "", IPStart = parts[0], IPEnd = parts[0], FQDN = "", Subnet = "" });
                        }
                        else
                        {
                            result.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = subnet });
                        }
                    }
                }
                else if (type == "iprange")
                {
                    var range = obj["range"]?.ToString();
                    if (!string.IsNullOrEmpty(range))
                    {
                        var parts = range.Split('-');
                        result.Add(new { Group = "", IPStart = parts[0].Trim(), IPEnd = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim(), FQDN = "", Subnet = "" });
                    }
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

                if (type == "group")
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
                else
                {
                    var protocol = obj["protocol"]?.ToString() ?? "TCP";
                    var portRange = obj["tcp-portrange"]?.ToString() ?? obj["udp-portrange"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(portRange))
                    {
                        var parts = portRange.Split('-');
                        int? portS = int.TryParse(parts[0], out int ps) ? ps : (int?)null;
                        int? portE = parts.Length > 1 && int.TryParse(parts[1], out int pe) ? pe : portS;
                        result.Add(new { Group = "", Protocol = protocol.ToUpper(), PortS = portS, PortE = portE });
                    }
                    else
                    {
                        result.Add(new { Group = "", Protocol = protocol.ToUpper(), PortS = (int?)null, PortE = (int?)null });
                    }
                }
            }

            return result;
        }

        private List<dynamic> FetchIPGroupFromAPI(string groupName)
        {
            Logger.LogDebug($"[FortiGate] 模擬從API取得IP群組: {groupName}");
            return new List<dynamic>
            {
                new { Group = "", IPStart = "10.0.0.1", IPEnd = "10.0.0.254", FQDN = "", Subnet = "" }
            };
        }

        private List<dynamic> FetchServiceGroupFromAPI(string groupName)
        {
            Logger.LogDebug($"[FortiGate] 模擬從API取得服務群組: {groupName}");
            return new List<dynamic>
            {
                new { Group = "", Protocol = "TCP", PortS = (int?)443, PortE = (int?)443 }
            };
        }
    }
}
