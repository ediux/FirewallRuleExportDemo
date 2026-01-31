using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class ApiUserInfo : IApiUserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string AuthType { get; set; }
        public string AdditionalInfo { get; set; }
        public string Token { get; set; }
    }
}
