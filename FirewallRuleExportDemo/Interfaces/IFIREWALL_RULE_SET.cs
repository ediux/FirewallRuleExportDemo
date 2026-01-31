using System;

namespace FirewallRuleExportDemo.Interfaces
{
    public interface IFIREWALL_RULE_SET
    {
        string DEVICE_NAME { get; set; }
        string RULENO { get; set; }
        string UUID { get; set; }
        int SAMEKEY { get; set; }
        string INCOMING_INTERFACE { get; set; }
        string OUTGOING_INTERFACE { get; set; }
        string SOURCE_GROUP { get; set; }
        string SOURCE_IP_START { get; set; }
        string SOURCE_IP_END { get; set; }
        string SOURCE_FQDN { get; set; }
        string SOURCE_SUBNET { get; set; }
        string DESTINATION_GROUP { get; set; }
        string DESTINATION_IP_START { get; set; }
        string DESTINATION_IP_END { get; set; }
        string DESTINATION_FQDN { get; set; }
        string DESTINATION_SUBNET1 { get; set; }
        string PROTOCAL { get; set; }
        int? PORT_S { get; set; }
        int? PORT_E { get; set; }
        string SERVICE_GROUP { get; set; }
        string ACTION { get; set; }
        bool ENABLED { get; set; }
        string DATA_DT { get; set; }
        int? HIT { get; set; }
        string FIRST_HIT_DATE { get; set; }
        string LAST_HIT_DATE { get; set; }
        string LayerObject { get; set; }
        string VDOM { get; set; }
        bool? HAS_EXPIRE_DATE { get; set; }
        string EXPIRE_DATE { get; set; }
        string COMMENT { get; set; }
    }
}
