using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class ServiceInformation : IServiceInformation
    {
        public string Name { get; set; }
        public string Protocol { get; set; }
        public string PortS { get; set; }
        public string PortE { get; set; }
    }
}
