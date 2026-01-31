using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Models;
using FirewallRuleExportDemo.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FirewallRuleExportDemo.Implementation
{
    /// <summary>
    /// CheckPoint 防火牆 API 客戶端 - 實際 API 呼叫版本
    /// Real CheckPoint Firewall API Client Implementation
    /// 
    /// 官方文件參考 / Official Documentation:
    /// https://sc1.checkpoint.com/documents/latest/APIs/
    /// 
    /// 支援版本 / Supported Versions:
    /// - R80.x 以上 / R80.x and above
    /// - R81.x
    /// 
    /// 認證方式 / Authentication:
    /// - 使用者名稱/密碼 / Username/Password
    /// - API Token (建議使用 / Recommended)
    /// </summary>
    public class CheckPointRealAPIClient : IFirewallAPIClient
    {
        private readonly IFirewallDeviceInfo _device;
        private readonly ICMDBSource _cmdbSource;
        private readonly HttpClient _httpClient;
        private string _sessionId;
        private readonly int _maxRetryCount;
        private readonly int _retryDelayMilliseconds;

        public CheckPointRealAPIClient(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
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
        }

        public IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device)
        {
            return new CheckPointRealAPIClient(device, _cmdbSource);
        }

        public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
        {
            var rules = new List<IFIREWALL_RULE_SET>();

            try
            {
                Logger.LogInfo($"[CheckPoint] 開始取得防火牆規則 - {_device.DeviceName} ({_device.DeviceIP})");

                // 1. 登入並取得 Session
                await Login();

                // 2. 從 CMDB 取得群組資訊
                var cmdbIPGroups = _cmdbSource.GetIPGroupInformation();
                var cmdbServiceGroups = _cmdbSource.GetServiceGroupInformation();

                Logger.LogInfo($"[CheckPoint] CMDB 中有 {cmdbIPGroups.Count} 個 IP 群組, {cmdbServiceGroups.Count} 個服務群組");

                // 3. 取得所有 Policy Packages
                var policyPackages = await GetPolicyPackages();
                Logger.LogInfo($"[CheckPoint] 找到 {policyPackages.Count} 個 Policy Package");

                // 4. 針對每個 Policy Package 取得規則
                int sameKey = 1;
                foreach (var package in policyPackages)
                {
                    var packageName = package["name"]?.ToString();
                    Logger.LogInfo($"[CheckPoint] 處理 Policy Package: {packageName}");

                    var apiRules = await FetchAccessRulebase(packageName);
                    Logger.LogInfo($"[CheckPoint] Package '{packageName}' 包含 {apiRules.Count} 條規則");

                    foreach (var apiRule in apiRules)
                    {
                        var expandedRules = ExpandRule(apiRule, cmdbIPGroups, cmdbServiceGroups, sameKey);
                        rules.AddRange(expandedRules);
                        sameKey += expandedRules.Count;
                    }
                }

                // 5. 登出
                await Logout();

                Logger.LogInfo($"[CheckPoint] 成功取得 {rules.Count} 筆防火牆規則");
                return rules;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CheckPoint] 取得防火牆規則時發生錯誤", ex);
                
                // 嘗試登出 (即使發生錯誤)
                try
                {
                    if (!string.IsNullOrEmpty(_sessionId))
                    {
                        await Logout();
                    }
                }
                catch { }
                
                throw;
            }
        }

        /// <summary>
        /// 登入 CheckPoint Management API
        /// Login to CheckPoint Management API
        /// </summary>
        private async Task Login()
        {
            var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Login_Session);
            var userInfo = _cmdbSource.GetAPIUser(_device);

            Logger.LogDebug($"[CheckPoint] 嘗試登入 API: {apiInfo.APIURL}");

            // 準備登入資料
            var loginData = new Dictionary<string, object>();

            // 判斷使用何種認證方式
            if (!string.IsNullOrEmpty(userInfo.Token))
            {
                // 使用 API Key 認證 (建議方式)
                loginData["api-key"] = userInfo.Token;
                Logger.LogDebug("[CheckPoint] 使用 API Key 認證");
            }
            else
            {
                // 使用帳號密碼認證
                loginData["user"] = userInfo.UserName;
                loginData["password"] = userInfo.Password;
                Logger.LogDebug($"[CheckPoint] 使用帳號密碼認證: {userInfo.UserName}");
            }

            // 設定其他登入參數
            loginData["continue-last-session"] = true;
            loginData["read-only"] = true; // 只讀模式

            var response = await ExecuteAPICall(
                apiInfo.APIURL,
                loginData,
                retryCount: _maxRetryCount
            );

            if (response["sid"] != null)
            {
                _sessionId = response["sid"].ToString();
                Logger.LogInfo($"[CheckPoint] 登入成功 - Session ID: {_sessionId.Substring(0, 8)}...");

                // 設定 Session Header
                _httpClient.DefaultRequestHeaders.Remove("X-chkp-sid");
                _httpClient.DefaultRequestHeaders.Add("X-chkp-sid", _sessionId);
            }
            else
            {
                throw new Exception("登入失敗: 未取得 Session ID");
            }
        }

        /// <summary>
        /// 登出 CheckPoint Management API
        /// Logout from CheckPoint Management API
        /// </summary>
        private async Task Logout()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Logout_Session);
                Logger.LogDebug("[CheckPoint] 嘗試登出...");

                await ExecuteAPICall(apiInfo.APIURL, new Dictionary<string, object>());

                Logger.LogInfo("[CheckPoint] 登出成功");
                _sessionId = null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[CheckPoint] 登出時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得所有 Policy Packages
        /// Get all Policy Packages
        /// </summary>
        private async Task<List<JObject>> GetPolicyPackages()
        {
            var packages = new List<JObject>();

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Get_FirewallPolicyPackages);
                
                // 如果沒有設定 API,使用預設 URL
                if (string.IsNullOrEmpty(apiInfo.APIURL))
                {
                    apiInfo.APIURL = $"https://{_device.DeviceIP}/web_api/show-packages";
                }

                var requestData = new Dictionary<string, object>
                {
                    ["limit"] = 500,
                    ["details-level"] = "standard"
                };

                var response = await ExecuteAPICall(apiInfo.APIURL, requestData);

                if (response["packages"] is JArray packagesArray)
                {
                    foreach (var package in packagesArray)
                    {
                        packages.Add((JObject)package);
                    }
                }

                return packages;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[CheckPoint] 無法取得 Policy Packages: {ex.Message}");
                Logger.LogInfo("[CheckPoint] 將使用預設的 'Standard' package");
                
                // 回傳預設 package
                return new List<JObject>
                {
                    new JObject { ["name"] = "Standard" }
                };
            }
        }

        /// <summary>
        /// 取得指定 Policy Package 的 Access Rulebase
        /// Get Access Rulebase for specified Policy Package
        /// </summary>
        private async Task<List<JObject>> FetchAccessRulebase(string packageName)
        {
            var rules = new List<JObject>();

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Get_FirewallRules);
                
                // 如果沒有設定 API,使用預設 URL
                if (string.IsNullOrEmpty(apiInfo.APIURL))
                {
                    apiInfo.APIURL = $"https://{_device.DeviceIP}/web_api/show-access-rulebase";
                }

                int offset = 0;
                int limit = 50; // 每次取得 50 筆
                bool hasMore = true;

                while (hasMore)
                {
                    Logger.LogDebug($"[CheckPoint] 取得規則 offset: {offset}");

                    var requestData = new Dictionary<string, object>
                    {
                        ["name"] = packageName,
                        ["offset"] = offset,
                        ["limit"] = limit,
                        ["details-level"] = "full",
                        ["use-object-dictionary"] = true,
                        ["show-hits"] = true
                    };

                    var response = await ExecuteAPICall(apiInfo.APIURL, requestData);

                    // 解析規則
                    if (response["rulebase"] is JArray rulebaseArray)
                    {
                        foreach (var ruleItem in rulebaseArray)
                        {
                            if (ruleItem["rulebase"] is JArray nestedRules)
                            {
                                foreach (var rule in nestedRules)
                                {
                                    rules.Add((JObject)rule);
                                }
                            }
                            else
                            {
                                rules.Add((JObject)ruleItem);
                            }
                        }
                    }

                    // 檢查是否還有更多資料
                    var total = response["total"]?.Value<int>() ?? 0;
                    offset += limit;
                    hasMore = offset < total;
                }

                Logger.LogInfo($"[CheckPoint] 從 '{packageName}' 取得 {rules.Count} 條規則");
                return rules;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CheckPoint] 取得 Access Rulebase 時發生錯誤: {ex.Message}", ex);
                return rules;
            }
        }

        /// <summary>
        /// 展開規則 (處理群組)
        /// Expand rule (handle groups)
        /// </summary>
        private List<IFIREWALL_RULE_SET> ExpandRule(
            JObject apiRule, 
            List<IIPGroupInformation> cmdbIPGroups, 
            List<IServiceInformation> cmdbServiceGroups, 
            int baseSameKey)
        {
            var expandedRules = new List<IFIREWALL_RULE_SET>();

            try
            {
                var ruleNo = apiRule["rule-number"]?.ToString();
                var uuid = apiRule["uid"]?.ToString();
                var action = apiRule["action"]?["name"]?.ToString();
                var enabled = apiRule["enabled"]?.Value<bool>() ?? true;
                var comment = apiRule["comments"]?.ToString();
                var layer = apiRule["layer"]?.ToString();

                // 取得來源、目的、服務
                var sources = apiRule["source"] as JArray ?? new JArray();
                var destinations = apiRule["destination"] as JArray ?? new JArray();
                var services = apiRule["service"] as JArray ?? new JArray();

                // 取得命中統計
                var hits = apiRule["hits"]?["value"]?.Value<int>();
                var firstHit = apiRule["hits"]?["first-date"]?.Value<DateTime>();
                var lastHit = apiRule["hits"]?["last-date"]?.Value<DateTime>();

                // 展開 IP 物件
                var sourceList = ExpandIPObjects(sources, cmdbIPGroups);
                var destList = ExpandIPObjects(destinations, cmdbIPGroups);
                var serviceList = ExpandServiceObjects(services, cmdbServiceGroups);

                // 如果沒有任何項目,新增 Any
                if (sourceList.Count == 0) 
                    sourceList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                if (destList.Count == 0) 
                    destList.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                if (serviceList.Count == 0) 
                    serviceList.Add(new { Group = "", Protocol = "Any", PortS = (int?)null, PortE = (int?)null });

                // 建立笛卡爾乘積 (Cartesian Product)
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
                                HIT = hits,
                                FIRST_HIT_DATE = firstHit?.ToString("yyyy-MM-dd HH:mm:ss"),
                                LAST_HIT_DATE = lastHit?.ToString("yyyy-MM-dd HH:mm:ss"),
                                LayerObject = layer,
                                COMMENT = comment
                            };

                            expandedRules.Add(rule);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CheckPoint] 展開規則時發生錯誤", ex);
            }

            return expandedRules;
        }

        /// <summary>
        /// 展開 IP 物件 (處理 host, network, group 等)
        /// Expand IP objects (handle host, network, group, etc.)
        /// </summary>
        private List<dynamic> ExpandIPObjects(JArray ipObjects, List<IIPGroupInformation> cmdbIPGroups)
        {
            var result = new List<dynamic>();

            foreach (var obj in ipObjects)
            {
                var name = obj["name"]?.ToString();
                var type = obj["type"]?.ToString();

                // 特殊物件處理
                if (name == "Any")
                {
                    continue; // Any 不需要展開
                }

                switch (type)
                {
                    case "group":
                    case "CpmiAnyObject":
                        // 檢查是否在 CMDB 中
                        var groupInCMDB = cmdbIPGroups.Where(g => g.Name == name).ToList();
                        if (groupInCMDB.Any())
                        {
                            // 保留群組參考
                            result.Add(new { Group = name, IPStart = "", IPEnd = "", FQDN = "", Subnet = "" });
                            Logger.LogDebug($"[CheckPoint] IP 群組 '{name}' 存在於 CMDB，不展開");
                        }
                        else
                        {
                            // 從 API 取得群組成員並展開
                            Logger.LogDebug($"[CheckPoint] IP 群組 '{name}' 不在 CMDB，從 API 取得");
                            var groupMembers = FetchIPGroupFromAPI(name).Result;
                            result.AddRange(groupMembers);
                        }
                        break;

                    case "host":
                        var ipv4 = obj["ipv4-address"]?.ToString();
                        if (!string.IsNullOrEmpty(ipv4))
                        {
                            result.Add(new { Group = "", IPStart = ipv4, IPEnd = ipv4, FQDN = "", Subnet = "" });
                        }
                        break;

                    case "network":
                        var subnet4 = obj["subnet4"]?.ToString();
                        var maskLength = obj["mask-length4"]?.ToString();
                        if (!string.IsNullOrEmpty(subnet4))
                        {
                            var cidr = !string.IsNullOrEmpty(maskLength) ? $"{subnet4}/{maskLength}" : subnet4;
                            result.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = "", Subnet = cidr });
                        }
                        break;

                    case "address-range":
                        var ipv4From = obj["ipv4-address-first"]?.ToString();
                        var ipv4To = obj["ipv4-address-last"]?.ToString();
                        if (!string.IsNullOrEmpty(ipv4From))
                        {
                            result.Add(new { 
                                Group = "", 
                                IPStart = ipv4From, 
                                IPEnd = ipv4To ?? ipv4From, 
                                FQDN = "", 
                                Subnet = "" 
                            });
                        }
                        break;

                    case "dns-domain":
                        var fqdn = obj["name"]?.ToString();
                        if (!string.IsNullOrEmpty(fqdn))
                        {
                            result.Add(new { Group = "", IPStart = "", IPEnd = "", FQDN = fqdn, Subnet = "" });
                        }
                        break;

                    default:
                        Logger.LogWarning($"[CheckPoint] 未處理的 IP 物件類型: {type} ({name})");
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 展開服務物件 (處理 tcp, udp, service-group 等)
        /// Expand service objects (handle tcp, udp, service-group, etc.)
        /// </summary>
        private List<dynamic> ExpandServiceObjects(JArray serviceObjects, List<IServiceInformation> cmdbServiceGroups)
        {
            var result = new List<dynamic>();

            foreach (var obj in serviceObjects)
            {
                var name = obj["name"]?.ToString();
                var type = obj["type"]?.ToString();

                // 特殊物件處理
                if (name == "Any")
                {
                    continue; // Any 不需要展開
                }

                switch (type)
                {
                    case "service-group":
                    case "CpmiAnyObject":
                        // 檢查是否在 CMDB 中
                        var groupInCMDB = cmdbServiceGroups.Where(g => g.Name == name).ToList();
                        if (groupInCMDB.Any())
                        {
                            // 保留群組參考
                            result.Add(new { Group = name, Protocol = "", PortS = (int?)null, PortE = (int?)null });
                            Logger.LogDebug($"[CheckPoint] 服務群組 '{name}' 存在於 CMDB，不展開");
                        }
                        else
                        {
                            // 從 API 取得群組成員並展開
                            Logger.LogDebug($"[CheckPoint] 服務群組 '{name}' 不在 CMDB，從 API 取得");
                            var groupMembers = FetchServiceGroupFromAPI(name).Result;
                            result.AddRange(groupMembers);
                        }
                        break;

                    case "service-tcp":
                        var tcpPort = obj["port"]?.ToString();
                        if (!string.IsNullOrEmpty(tcpPort))
                        {
                            ParsePortRange(tcpPort, out int? portS, out int? portE);
                            result.Add(new { Group = "", Protocol = "TCP", PortS = portS, PortE = portE });
                        }
                        break;

                    case "service-udp":
                        var udpPort = obj["port"]?.ToString();
                        if (!string.IsNullOrEmpty(udpPort))
                        {
                            ParsePortRange(udpPort, out int? portS, out int? portE);
                            result.Add(new { Group = "", Protocol = "UDP", PortS = portS, PortE = portE });
                        }
                        break;

                    case "service-icmp":
                        var icmpType = obj["icmp-type"]?.ToString();
                        result.Add(new { Group = "", Protocol = "ICMP", PortS = (int?)null, PortE = (int?)null });
                        break;

                    case "service-other":
                        var ipProtocol = obj["ip-protocol"]?.ToString();
                        result.Add(new { Group = "", Protocol = ipProtocol ?? "Other", PortS = (int?)null, PortE = (int?)null });
                        break;

                    default:
                        Logger.LogWarning($"[CheckPoint] 未處理的服務物件類型: {type} ({name})");
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 從 API 取得 IP 群組成員
        /// Fetch IP group members from API
        /// </summary>
        private async Task<List<dynamic>> FetchIPGroupFromAPI(string groupName)
        {
            var members = new List<dynamic>();

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Get_IPGroupInformation);
                
                if (string.IsNullOrEmpty(apiInfo.APIURL))
                {
                    apiInfo.APIURL = $"https://{_device.DeviceIP}/web_api/show-group";
                }

                var requestData = new Dictionary<string, object>
                {
                    ["name"] = groupName,
                    ["details-level"] = "full"
                };

                var response = await ExecuteAPICall(apiInfo.APIURL, requestData);

                if (response["members"] is JArray membersArray)
                {
                    foreach (var member in membersArray)
                    {
                        var memberType = member["type"]?.ToString();
                        var memberName = member["name"]?.ToString();

                        // 遞迴展開群組成員
                        if (memberType == "group")
                        {
                            var subMembers = await FetchIPGroupFromAPI(memberName);
                            members.AddRange(subMembers);
                        }
                        else
                        {
                            var expanded = ExpandIPObjects(new JArray { member }, new List<IIPGroupInformation>());
                            members.AddRange(expanded);
                        }
                    }
                }

                Logger.LogDebug($"[CheckPoint] IP 群組 '{groupName}' 包含 {members.Count} 個成員");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[CheckPoint] 無法取得 IP 群組 '{groupName}': {ex.Message}");
            }

            return members;
        }

        /// <summary>
        /// 從 API 取得服務群組成員
        /// Fetch service group members from API
        /// </summary>
        private async Task<List<dynamic>> FetchServiceGroupFromAPI(string groupName)
        {
            var members = new List<dynamic>();

            try
            {
                var apiInfo = _cmdbSource.GetAPIInformation(_device, APINames.Get_ServiceGroupInformation);
                
                if (string.IsNullOrEmpty(apiInfo.APIURL))
                {
                    apiInfo.APIURL = $"https://{_device.DeviceIP}/web_api/show-service-group";
                }

                var requestData = new Dictionary<string, object>
                {
                    ["name"] = groupName,
                    ["details-level"] = "full"
                };

                var response = await ExecuteAPICall(apiInfo.APIURL, requestData);

                if (response["members"] is JArray membersArray)
                {
                    foreach (var member in membersArray)
                    {
                        var memberType = member["type"]?.ToString();
                        var memberName = member["name"]?.ToString();

                        // 遞迴展開群組成員
                        if (memberType == "service-group")
                        {
                            var subMembers = await FetchServiceGroupFromAPI(memberName);
                            members.AddRange(subMembers);
                        }
                        else
                        {
                            var expanded = ExpandServiceObjects(new JArray { member }, new List<IServiceInformation>());
                            members.AddRange(expanded);
                        }
                    }
                }

                Logger.LogDebug($"[CheckPoint] 服務群組 '{groupName}' 包含 {members.Count} 個成員");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[CheckPoint] 無法取得服務群組 '{groupName}': {ex.Message}");
            }

            return members;
        }

        /// <summary>
        /// 執行 API 呼叫 (包含重試機制)
        /// Execute API call (with retry mechanism)
        /// </summary>
        private async Task<JObject> ExecuteAPICall(
            string url, 
            Dictionary<string, object> data, 
            int retryCount = 0)
        {
            int attempt = 0;
            Exception lastException = null;

            while (attempt <= retryCount)
            {
                try
                {
                    if (attempt > 0)
                    {
                        Logger.LogDebug($"[CheckPoint] 重試第 {attempt} 次...");
                        await Task.Delay(_retryDelayMilliseconds);
                    }

                    var jsonContent = JsonConvert.SerializeObject(data);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    Logger.LogTrace($"[CheckPoint] POST {url}");
                    Logger.LogTrace($"[CheckPoint] Request: {jsonContent}");

                    var response = await _httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    Logger.LogTrace($"[CheckPoint] Response Status: {response.StatusCode}");
                    Logger.LogTrace($"[CheckPoint] Response: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(responseContent);
                        
                        // 檢查是否有錯誤訊息
                        if (result["message"] != null && result["code"] != null)
                        {
                            var code = result["code"]?.ToString();
                            var message = result["message"]?.ToString();
                            
                            if (code != "generic_ok")
                            {
                                throw new Exception($"API 回傳錯誤: [{code}] {message}");
                            }
                        }

                        return result;
                    }
                    else
                    {
                        throw new Exception($"HTTP 錯誤: {response.StatusCode} - {responseContent}");
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger.LogWarning($"[CheckPoint] API 呼叫失敗 (嘗試 {attempt + 1}/{retryCount + 1}): {ex.Message}");
                    attempt++;
                }
            }

            throw new Exception($"API 呼叫失敗，已重試 {retryCount} 次", lastException);
        }

        /// <summary>
        /// 解析埠號範圍
        /// Parse port range
        /// </summary>
        private void ParsePortRange(string portString, out int? portS, out int? portE)
        {
            portS = null;
            portE = null;

            if (string.IsNullOrEmpty(portString))
                return;

            // 處理範圍格式: "80-90" 或 ">1024" 或 "80"
            if (portString.Contains("-"))
            {
                var parts = portString.Split('-');
                if (int.TryParse(parts[0], out int p1))
                    portS = p1;
                if (parts.Length > 1 && int.TryParse(parts[1], out int p2))
                    portE = p2;
            }
            else if (portString.StartsWith(">"))
            {
                if (int.TryParse(portString.Substring(1), out int p))
                {
                    portS = p + 1;
                    portE = 65535;
                }
            }
            else if (portString.StartsWith("<"))
            {
                if (int.TryParse(portString.Substring(1), out int p))
                {
                    portS = 1;
                    portE = p - 1;
                }
            }
            else
            {
                if (int.TryParse(portString, out int p))
                {
                    portS = p;
                    portE = p;
                }
            }
        }
    }
}
