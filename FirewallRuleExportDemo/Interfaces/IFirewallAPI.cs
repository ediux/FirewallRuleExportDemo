namespace FirewallRuleExportDemo.Interfaces
{
    public interface IFirewallAPI
    {
        IFirewallDeviceInfo FirewallDeviceInfo { get; set; }
        string APIName { get; set; }
        string APIURL { get; set; }
    }
}
