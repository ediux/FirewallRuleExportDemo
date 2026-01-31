using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Models;
using FirewallRuleExportDemo.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FirewallRuleExportDemo.Implementation
{
    /// <summary>
    /// FortiGate 防火牆 API 客戶端 - 實際 API 呼叫版本
    /// Real FortiGate Firewall API Client Implementation
    /// 
    /// 官方文件參考 / Official Documentation:
    /// https://docs.fortinet.com/document/fortigate/7.0.0/administration-guide/
    /// https://docs.fortinet.com/document/fortigate/7.0.0/rest-api-reference/
    /// 
    /// 支援版本 / Supported Versions:
    /// - FortiGate 6.x 以上 / FortiGate 6.x and above
    /// - FortiGate 7.x
    /// 
    /// 認證方式 / Authentication:
    /// - API Token (主要方式 / Primary Method)
    /// </summary>
    public class FortiGateRealAPIClient : IFirewallAPIClient
    {
        private readonly IFirewallDeviceInfo _device;
        private readonly ICMDBSource _cmdbSource;
        private readonly HttpClient _httpClient;
        private readonly int _maxRetryCount;
        private readonly int _retryDelayMilliseconds;
        private string _apiToken;

        public FortiGateRealAPIClient(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
        {
            _device = device;
            _cmdbSource = cmdbSource;
            _maxRetryCount = cmdbSource.GetMaxRetryCount();
            _retryDelayMilliseconds = cmdbSource.GetRetryDelayMilliseconds();

            // 設定 HttpClient
            var handler = new HttpClientHandler();
            
            // 忽略 SSL 憑證驗證 (生產環境應該使用有效憑證)
            handler.ServerCertificateCustomValidationCallback = 
                (sender, cert, chain, sslPolicyErrors) => true;

            // 設定 Proxy (如果需要)
            if (cmdbSource.GetUseProxy())
            {
                handler.Proxy = new WebProxy(
                    cmdbSource.GetProxyAddress(), 
                    cmdbSource.GetProxyPort()
                );
                
                if (!string.IsNullOrEmpty(cmdbSource.GetProxyUserName()))
                {
                    handler.Proxy.Credentials = new NetworkCredential(
                        cmdbSource.GetProxyUserName(),
                        cmdbSource.GetProxyPassword()
                    );
                }
            }

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(cmdbSource.GetAPITimeout())
            };

            // 設定預設 Headers
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );

            // 取得 API Token
            var userInfo = _cmdbSource.GetAPIUser(_device);
            _apiToken = userInfo.Token;

            if (string.IsNullOrEmpty(_apiToken))
            {
                throw new Exception("FortiGate API 需要 API Token，請在 CMDB 中設定");
            }
        }

        public IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device)
        {
            return new FortiGateRealAPIClient(device, _cmdbSource);
        }

        public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
        {
            var rules = new List<IFIREWALL_RULE_SET>();

            try
            {
                Logger.LogInfo($"[FortiGate] 開始取得防火牆規則 - {_device.DeviceName} ({_device.DeviceIP})");

                // 1. 驗證 API Token
                if (!await ValidateAPIToken())
                {
                    throw new Exception("API Token 驗證失敗");
                }

                // 2. 從 CMDB 取得群組資訊
                var cmdbIPGroups = _cmdbSource.GetIPGroupInformation();
                var cmdbServiceGroups = _cmdbSource.GetServiceGroupInformation();

                Logger.LogInfo($"[FortiGate] CMDB 中有 {cmdbIPGroups.Count} 個 IP 群組, {cmdbServiceGroups.Count} 個服務群組");

                // 3. 取得所有 VDOM (Virtual Domain)
                var vdoms = await GetVDOMs();
                Logger.LogInfo($"[FortiGate] 找到 {vdoms.Count} 個 VDOM");

                // 4. 針對每個 VDOM 取得防火牆規則
                int sameKey = 1;
                foreach (var vdom in vdoms)
                {
                    var vdomName = vdom["name"]?.ToString();
                    Logger.LogInfo($"[FortiGate] 處理 VDOM: {vdomName}");

                    var apiRules = await FetchFirewallPolicies(vdomName);
                    Logger.LogInfo($"[FortiGate] VDOM '{vdomName}' 包含 {apiRules.Count} 條規則");

                    foreach (var apiRule in apiRules)
                    {
                        var expandedRules = await ExpandRule(apiRule, cmdbIPGroups, cmdbServiceGroups, sameKey, vdomName);
                        rules.AddRange(expandedRules);
                        sameKey += expandedRules.Count;
                    }
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

        private async Task<bool> ValidateAPIToken()
        {
            try
            {
                Logger.LogDebug("[FortiGate] 驗證 API Token...");

                var url = $"https://{_device.DeviceIP}/api/v2/monitor/system/status";
                var response = await ExecuteAPICall(url, HttpMethod.Get);

                if (response != null)
                {
                    var version = response["version"]?.ToString();
                    var hostname = response["results"]?["hostname"]?.ToString();
                    Logger.LogInfo($"[FortiGate] API Token 驗證成功 - 主機: {hostname}, 版本: {version}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError("[FortiGate] API Token 驗證失敗", ex);
                return false;
            }
        }

        private async Task<List<JObject>> GetVDOMs()
        {
            var vdoms = new List<JObject>();

            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/system/vdom";
                var response = await ExecuteAPICall(url, HttpMethod.Get);

                if (response["results"] is JArray vdomArray)
                {
                    foreach (var vdom in vdomArray)
                    {
                        vdoms.Add((JObject)vdom);
                    }
                }

                if (vdoms.Count == 0)
                {
                    Logger.LogInfo("[FortiGate] 使用預設 VDOM: root");
                    vdoms.Add(new JObject { ["name"] = "root" });
                }

                return vdoms;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[FortiGate] 無法取得 VDOM 列表: {ex.Message}");
                Logger.LogInfo("[FortiGate] 將使用預設 VDOM: root");
                
                return new List<JObject>
                {
                    new JObject { ["name"] = "root" }
                };
            }
        }

        private async Task<List<JObject>> FetchFirewallPolicies(string vdomName)
        {
            var policies = new List<JObject>();

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Get_FirewallRules);
                
                var url = string.IsNullOrEmpty(apiInfo?.APIURL)
                    ? $"https://{_device.DeviceIP}/api/v2/cmdb/firewall/policy"
                    : apiInfo.APIURL;

                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                Logger.LogDebug($"[FortiGate] 取得防火牆規則: {url}");

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName);

                if (response["results"] is JArray policiesArray)
                {
                    foreach (var policy in policiesArray)
                    {
                        policies.Add((JObject)policy);
                    }
                }

                Logger.LogInfo($"[FortiGate] 從 VDOM '{vdomName}' 取得 {policies.Count} 條規則");
                return policies;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[FortiGate] 取得防火牆規則時發生錯誤: {ex.Message}", ex);
                return policies;
            }
        }

        private async Task<List<IFIREWALL_RULE_SET>> ExpandRule(
            JObject apiRule,
            List<IIPGroupInformation> cmdbIPGroups,
            List<IServiceInformation> cmdbServiceGroups,
            int baseSameKey,
            string vdomName)
        {
            var expandedRules = new List<IFIREWALL_RULE_SET>();

            try
            {
                var policyId = apiRule["policyid"]?.ToString();
                var uuid = apiRule["uuid"]?.ToString();
                var name = apiRule["name"]?.ToString();
                var action = apiRule["action"]?.ToString();
                var status = apiRule["status"]?.ToString();
                var enabled = status == "enable";
                var comment = apiRule["comments"]?.ToString();

                var srcIntf = apiRule["srcintf"] as JArray ?? new JArray();
                var dstIntf = apiRule["dstintf"] as JArray ?? new JArray();
                var srcIntfName = srcIntf.Count > 0 ? srcIntf[0]["name"]?.ToString() : "";
                var dstIntfName = dstIntf.Count > 0 ? dstIntf[0]["name"]?.ToString() : "";

                var sources = apiRule["srcaddr"] as JArray ?? new JArray();
                var destinations = apiRule["dstaddr"] as JArray ?? new JArray();
                var services = apiRule["service"] as JArray ?? new JArray();

                var sourceList = await ExpandIPObjects(sources, cmdbIPGroups, vdomName);
                var destList = await ExpandIPObjects(destinations, cmdbIPGroups, vdomName);
                var serviceList = await ExpandServiceObjects(services, cmdbServiceGroups, vdomName);

                if (sourceList.Count == 0)
                    sourceList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                if (destList.Count == 0)
                    destList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                if (serviceList.Count == 0)
                    serviceList.Add(new { Group = "", Protocol = "Any", PortS = (int?)null, PortE = (int?)null });

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
                                RULENO = policyId,
                                UUID = uuid ?? $"fg-{policyId}",
                                SAMEKEY = sameKey++,
                                INCOMING_INTERFACE = srcIntfName,
                                OUTGOING_INTERFACE = dstIntfName,
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
                                VDOM = vdomName,
                                COMMENT = comment ?? name
                            };

                            expandedRules.Add(rule);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[FortiGate] 展開規則時發生錯誤", ex);
            }

            return expandedRules;
        }

        private async Task<List<dynamic>> ExpandIPObjects(
            JArray ipObjects,
            List<IIPGroupInformation> cmdbIPGroups,
            string vdomName)
        {
            var result = new List<dynamic>();

            foreach (var obj in ipObjects)
            {
                var name = obj["name"]?.ToString();

                if (name == "all" || name == "any")
                {
                    continue;
                }

                var isGroup = await IsAddressGroup(name, vdomName);

                if (isGroup)
                {
                    var groupInCMDB = cmdbIPGroups.Where(g => g.Name == name).ToList();
                    if (groupInCMDB.Any())
                    {
                        result.Add(new { Group = name, IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                        Logger.LogDebug($"[FortiGate] IP 群組 '{name}' 存在於 CMDB，不展開");
                    }
                    else
                    {
                        Logger.LogDebug($"[FortiGate] IP 群組 '{name}' 不在 CMDB，從 API 取得");
                        var groupMembers = await FetchAddressGroupFromAPI(name, vdomName);
                        result.AddRange(groupMembers);
                    }
                }
                else
                {
                    var addressDetail = await FetchAddressObjectFromAPI(name, vdomName);
                    if (addressDetail != null)
                    {
                        result.Add(addressDetail);
                    }
                }
            }

            return result;
        }

        private async Task<List<dynamic>> ExpandServiceObjects(
            JArray serviceObjects,
            List<IServiceInformation> cmdbServiceGroups,
            string vdomName)
        {
            var result = new List<dynamic>();

            foreach (var obj in serviceObjects)
            {
                var name = obj["name"]?.ToString();

                if (name == "ALL" || name == "all")
                {
                    continue;
                }

                var isGroup = await IsServiceGroup(name, vdomName);

                if (isGroup)
                {
                    var groupInCMDB = cmdbServiceGroups.Where(g => g.Name == name).ToList();
                    if (groupInCMDB.Any())
                    {
                        result.Add(new { Group = name, Protocol = "", PortS = (int?)null, PortE = (int?)null });
                        Logger.LogDebug($"[FortiGate] 服務群組 '{name}' 存在於 CMDB，不展開");
                    }
                    else
                    {
                        Logger.LogDebug($"[FortiGate] 服務群組 '{name}' 不在 CMDB，從 API 取得");
                        var groupMembers = await FetchServiceGroupFromAPI(name, vdomName);
                        result.AddRange(groupMembers);
                    }
                }
                else
                {
                    var serviceDetail = await FetchServiceObjectFromAPI(name, vdomName);
                    if (serviceDetail != null)
                    {
                        result.Add(serviceDetail);
                    }
                }
            }

            return result;
        }

        private async Task<bool> IsAddressGroup(string name, string vdomName)
        {
            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall/addrgrp/{Uri.EscapeDataString(name)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName, throwOnError: false);
                return response != null && response["results"] != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsServiceGroup(string name, string vdomName)
        {
            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall.service/group/{Uri.EscapeDataString(name)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName, throwOnError: false);
                return response != null && response["results"] != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<dynamic> FetchAddressObjectFromAPI(string name, string vdomName)
        {
            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall/address/{Uri.EscapeDataString(name)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName);

                if (response["results"] is JArray resultsArray && resultsArray.Count > 0)
                {
                    var addr = resultsArray[0];
                    var type = addr["type"]?.ToString();

                    switch (type)
                    {
                        case "ipmask":
                            var subnet = addr["subnet"]?.ToString();
                            if (!string.IsNullOrEmpty(subnet))
                            {
                                var parts = subnet.Split(' ');
                                if (parts.Length == 2)
                                {
                                    if (parts[1] == "255.255.255.255")
                                    {
                                        return new { Group = "", IPStart = parts[0], IPEnd = parts[0], FQDN = "", Subnet = "" };
                                    }
                                    else
                                    {
                                        var cidr = ConvertSubnetMaskToCIDR(parts[0], parts[1]);
                                        return new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = cidr };
                                    }
                                }
                            }
                            break;

                        case "iprange":
                            var startIp = addr["start-ip"]?.ToString();
                            var endIp = addr["end-ip"]?.ToString();
                            if (!string.IsNullOrEmpty(startIp))
                            {
                                return new { Group = "", IPStart = startIp, IPEnd = endIp ?? startIp, FQDN = "", Subnet = "" };
                            }
                            break;

                        case "fqdn":
                            var fqdn = addr["fqdn"]?.ToString();
                            if (!string.IsNullOrEmpty(fqdn))
                            {
                                return new { Group = "", IPStart = "", IPEnd = "", FQDN = fqdn, Subnet = "" };
                            }
                            break;

                        case "geography":
                            var country = addr["country"]?.ToString();
                            Logger.LogDebug($"[FortiGate] 地理位置物件: {name} ({country})");
                            return new { Group = "", IPStart = "", IPEnd = "", FQDN = $"GEO:{country}", Subnet = "" };

                        default:
                            Logger.LogWarning($"[FortiGate] 未處理的地址類型: {type} ({name})");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[FortiGate] 無法取得地址物件 '{name}': {ex.Message}");
            }

            return null;
        }

        private async Task<List<dynamic>> FetchAddressGroupFromAPI(string groupName, string vdomName)
        {
            var members = new List<dynamic>();

            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall/addrgrp/{Uri.EscapeDataString(groupName)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName);

                if (response["results"] is JArray resultsArray && resultsArray.Count > 0)
                {
                    var group = resultsArray[0];
                    if (group["member"] is JArray membersArray)
                    {
                        foreach (var member in membersArray)
                        {
                            var memberName = member["name"]?.ToString();
                            
                            if (await IsAddressGroup(memberName, vdomName))
                            {
                                var subMembers = await FetchAddressGroupFromAPI(memberName, vdomName);
                                members.AddRange(subMembers);
                            }
                            else
                            {
                                var memberDetail = await FetchAddressObjectFromAPI(memberName, vdomName);
                                if (memberDetail != null)
                                {
                                    members.Add(memberDetail);
                                }
                            }
                        }
                    }
                }

                Logger.LogDebug($"[FortiGate] 地址群組 '{groupName}' 包含 {members.Count} 個成員");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[FortiGate] 無法取得地址群組 '{groupName}': {ex.Message}");
            }

            return members;
        }

        private async Task<dynamic> FetchServiceObjectFromAPI(string name, string vdomName)
        {
            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall.service/custom/{Uri.EscapeDataString(name)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName, throwOnError: false);

                if (response?["results"] is JArray resultsArray && resultsArray.Count > 0)
                {
                    var service = resultsArray[0];
                    var protocol = service["protocol"]?.ToString();
                    
                    if (protocol == "TCP/UDP/SCTP")
                    {
                        var tcpPortrange = service["tcp-portrange"]?.ToString();
                        var udpPortrange = service["udp-portrange"]?.ToString();

                        if (!string.IsNullOrEmpty(tcpPortrange))
                        {
                            ParsePortRange(tcpPortrange, out int? portS, out int? portE);
                            return new { Group = "", Protocol = "TCP", PortS = portS, PortE = portE };
                        }
                        
                        if (!string.IsNullOrEmpty(udpPortrange))
                        {
                            ParsePortRange(udpPortrange, out int? portS, out int? portE);
                            return new { Group = "", Protocol = "UDP", PortS = portS, PortE = portE };
                        }
                    }
                    else if (protocol == "ICMP")
                    {
                        return new { Group = "", Protocol = "ICMP", PortS = (int?)null, PortE = (int?)null };
                    }
                    else if (protocol == "IP")
                    {
                        var protocolNumber = service["protocol-number"]?.ToString();
                        return new { Group = "", Protocol = protocolNumber ?? "IP", PortS = (int?)null, PortE = (int?)null };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[FortiGate] 無法取得服務物件 '{name}': {ex.Message}");
            }

            return null;
        }

        private async Task<List<dynamic>> FetchServiceGroupFromAPI(string groupName, string vdomName)
        {
            var members = new List<dynamic>();

            try
            {
                var url = $"https://{_device.DeviceIP}/api/v2/cmdb/firewall.service/group/{Uri.EscapeDataString(groupName)}";
                if (!string.IsNullOrEmpty(vdomName) && vdomName != "root")
                {
                    url += $"?vdom={vdomName}";
                }

                var response = await ExecuteAPICall(url, HttpMethod.Get, vdomName);

                if (response["results"] is JArray resultsArray && resultsArray.Count > 0)
                {
                    var group = resultsArray[0];
                    if (group["member"] is JArray membersArray)
                    {
                        foreach (var member in membersArray)
                        {
                            var memberName = member["name"]?.ToString();
                            
                            if (await IsServiceGroup(memberName, vdomName))
                            {
                                var subMembers = await FetchServiceGroupFromAPI(memberName, vdomName);
                                members.AddRange(subMembers);
                            }
                            else
                            {
                                var memberDetail = await FetchServiceObjectFromAPI(memberName, vdomName);
                                if (memberDetail != null)
                                {
                                    members.Add(memberDetail);
                                }
                            }
                        }
                    }
                }

                Logger.LogDebug($"[FortiGate] 服務群組 '{groupName}' 包含 {members.Count} 個成員");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[FortiGate] 無法取得服務群組 '{groupName}': {ex.Message}");
            }

            return members;
        }

        private async Task<JObject> ExecuteAPICall(
            string url,
            HttpMethod method,
            string vdomName = null,
            int retryCount = 0,
            bool throwOnError = true)
        {
            int attempt = 0;
            Exception lastException = null;

            if (retryCount == 0)
            {
                retryCount = _maxRetryCount;
            }

            while (attempt <= retryCount)
            {
                try
                {
                    if (attempt > 0)
                    {
                        Logger.LogDebug($"[FortiGate] 重試第 {attempt} 次...");
                        await Task.Delay(_retryDelayMilliseconds);
                    }

                    var request = new HttpRequestMessage(method, url);
                    request.Headers.Add("Authorization", $"Bearer {_apiToken}");

                    Logger.LogTrace($"[FortiGate] {method} {url}");

                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Logger.LogTrace($"[FortiGate] Response Status: {response.StatusCode}");
                    Logger.LogTrace($"[FortiGate] Response: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(responseContent);
                        
                        var httpStatus = result["http_status"]?.Value<int>();
                        if (httpStatus.HasValue && httpStatus.Value >= 400)
                        {
                            var errorMsg = $"API 回傳錯誤: HTTP {httpStatus}";
                            if (throwOnError)
                            {
                                throw new Exception(errorMsg);
                            }
                            else
                            {
                                Logger.LogWarning($"[FortiGate] {errorMsg}");
                                return null;
                            }
                        }

                        return result;
                    }
                    else
                    {
                        var errorMsg = $"HTTP 錯誤: {response.StatusCode} - {responseContent}";
                        if (throwOnError)
                        {
                            throw new Exception(errorMsg);
                        }
                        else
                        {
                            Logger.LogWarning($"[FortiGate] {errorMsg}");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (throwOnError || attempt < retryCount)
                    {
                        Logger.LogWarning($"[FortiGate] API 呼叫失敗 (嘗試 {attempt + 1}/{retryCount + 1}): {ex.Message}");
                    }
                    
                    attempt++;
                    
                    if (!throwOnError && attempt > retryCount)
                    {
                        return null;
                    }
                }
            }

            if (throwOnError)
            {
                throw new Exception($"API 呼叫失敗，已重試 {retryCount} 次", lastException);
            }

            return null;
        }

        private string ConvertSubnetMaskToCIDR(string ipAddress, string subnetMask)
        {
            try
            {
                var maskParts = subnetMask.Split('.');
                if (maskParts.Length != 4)
                    return $"{ipAddress}/{subnetMask}";

                int cidr = 0;
                foreach (var part in maskParts)
                {
                    if (int.TryParse(part, out int value))
                    {
                        cidr += Convert.ToString(value, 2).Count(c => c == '1');
                    }
                }

                return $"{ipAddress}/{cidr}";
            }
            catch
            {
                return $"{ipAddress}/{subnetMask}";
            }
        }

        private void ParsePortRange(string portString, out int? portS, out int? portE)
        {
            portS = null;
            portE = null;

            if (string.IsNullOrEmpty(portString))
                return;

            var ranges = portString.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var range in ranges)
            {
                if (range.Contains("-"))
                {
                    var parts = range.Split('-');
                    if (int.TryParse(parts[0], out int p1))
                    {
                        portS = portS.HasValue ? Math.Min(portS.Value, p1) : p1;
                    }
                    if (parts.Length > 1 && int.TryParse(parts[1], out int p2))
                    {
                        portE = portE.HasValue ? Math.Max(portE.Value, p2) : p2;
                    }
                }
                else if (range.Contains(":"))
                {
                    var parts = range.Split(':');
                    if (int.TryParse(parts[0], out int p))
                    {
                        portS = portS.HasValue ? Math.Min(portS.Value, p) : p;
                        portE = portE.HasValue ? Math.Max(portE.Value, p) : p;
                    }
                }
                else
                {
                    if (int.TryParse(range, out int p))
                    {
                        portS = portS.HasValue ? Math.Min(portS.Value, p) : p;
                        portE = portE.HasValue ? Math.Max(portE.Value, p) : p;
                    }
                }
            }

            if (portS.HasValue && !portE.HasValue)
            {
                portE = portS;
            }
        }
    }
}
