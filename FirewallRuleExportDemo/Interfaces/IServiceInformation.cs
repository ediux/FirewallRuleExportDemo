namespace FirewallRuleExportDemo.Interfaces
{
    public interface IServiceInformation
    {
        string Name { get; set; }
        string Protocol { get; set; }
        string PortS { get; set; }
        string PortE { get; set; }
    }
}
