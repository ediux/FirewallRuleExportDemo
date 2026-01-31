using FirewallRuleExportDemo.Interfaces;

namespace FirewallRuleExportDemo.Models
{
    public class FirewallRuleSet : IFIREWALL_RULE_SET
    {
        public string DEVICE_NAME { get; set; }
        public string RULENO { get; set; }
        public string UUID { get; set; }
        public int SAMEKEY { get; set; }
        public string INCOMING_INTERFACE { get; set; }
        public string OUTGOING_INTERFACE { get; set; }
        public string SOURCE_GROUP { get; set; }
        public string SOURCE_IP_START { get; set; }
        public string SOURCE_IP_END { get; set; }
        public string SOURCE_FQDN { get; set; }
        public string SOURCE_SUBNET { get; set; }
        public string DESTINATION_GROUP { get; set; }
        public string DESTINATION_IP_START { get; set; }
        public string DESTINATION_IP_END { get; set; }
        public string DESTINATION_FQDN { get; set; }
        public string DESTINATION_SUBNET1 { get; set; }
        public string PROTOCAL { get; set; }
        public int? PORT_S { get; set; }
        public int? PORT_E { get; set; }
        public string SERVICE_GROUP { get; set; }
        public string ACTION { get; set; }
        public bool ENABLED { get; set; } = true;
        public string DATA_DT { get; set; }
        public int? HIT { get; set; }
        public string FIRST_HIT_DATE { get; set; }
        public string LAST_HIT_DATE { get; set; }
        public string LayerObject { get; set; }
        public string VDOM { get; set; }
        public bool? HAS_EXPIRE_DATE { get; set; }
        public string EXPIRE_DATE { get; set; }
        public string COMMENT { get; set; }
    }
}
