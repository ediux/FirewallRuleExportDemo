namespace FirewallRuleExportDemo.Interfaces
{
    public interface IIPGroupInformation
    {
        string Name { get; set; }
        byte? IpType { get; set; }
        string IpStart { get; set; }
        string IpEnd { get; set; }
        string IpSubnet { get; set; }
    }
}
