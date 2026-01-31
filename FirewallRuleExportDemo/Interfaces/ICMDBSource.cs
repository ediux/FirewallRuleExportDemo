using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirewallRuleExportDemo.Interfaces
{
    public interface ICMDBSource
    {
        List<IFirewallDeviceInfo> GetFirewallList();
        IApiUserInfo GetAPIUser(IFirewallDeviceInfo device);
        Task<List<IFIREWALL_RULE_SET>> DoFirewallRules(IFirewallDeviceInfo device);
        IFirewallAPI GetAPIInformation(IFirewallDeviceInfo device, APINames apiName);
        List<IIPGroupInformation> GetIPGroupInformation(string groupName = "");
        List<IServiceInformation> GetServiceGroupInformation(string groupName = "");
        Task<int> SaveFirewallRules(List<IFIREWALL_RULE_SET> rules);
        string GetDatabaseConnectionString();
        string GetCMDBConnectionString();
        int GetAPITimeout();
        int GetMaxRetryCount();
        int GetRetryDelayMilliseconds();
        bool GetUseProxy();
        string GetProxyAddress();
        int GetProxyPort();
        string GetProxyUserName();
        string GetProxyPassword();
        string GetLogFilePath();
        string GetLogLevel();
        bool GetEnableConsoleLog();
        bool GetEnableFileLog();
        void SendNotification(string subject, string message);
    }
}
