namespace FirewallRuleExportDemo.Interfaces
{
    public interface IApiUserInfo
    {
        string UserName { get; set; }
        string Password { get; set; }
        string AuthType { get; set; }
        string AdditionalInfo { get; set; }
        string Token { get; set; }
    }
}
