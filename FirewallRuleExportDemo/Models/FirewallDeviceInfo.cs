using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class FirewallDeviceInfo : IFirewallDeviceInfo
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public string FWVersion { get; set; }
        public string DeviceName { get; set; }
        public string DeviceIP { get; set; }
        public string Description { get; set; }
    }
}
