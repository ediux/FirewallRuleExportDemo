using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class FirewallAPI : IFirewallAPI
    {
        public IFirewallDeviceInfo FirewallDeviceInfo { get; set; }
        public string APIName { get; set; }
        public string APIURL { get; set; }
    }
}
