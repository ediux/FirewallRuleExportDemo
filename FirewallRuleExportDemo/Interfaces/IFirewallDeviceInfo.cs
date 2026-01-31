namespace FirewallRuleExportDemo.Interfaces
{
    public interface IFirewallDeviceInfo
    {
        string Brand { get; set; }
        string Model { get; set; }
        string FWVersion { get; set; }
        string DeviceName { get; set; }
        string DeviceIP { get; set; }
        string Description { get; set; }
    }
}
