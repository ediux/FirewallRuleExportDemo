using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class IPGroupInformation : IIPGroupInformation
    {
        public string Name { get; set; }
        public byte? IpType { get; set; }
        public string IpStart { get; set; }
        public string IpEnd { get; set; }
        public string IpSubnet { get; set; }
    }
}
