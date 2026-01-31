using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirewallRuleExportDemo.Interfaces
{
    public interface IFirewallAPIClient
    {
        Task<List<IFIREWALL_RULE_SET>> GetFirewallRules();
        IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device);
    }
}
