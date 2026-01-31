using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Utilities;

namespace FirewallRuleExportDemo.Services
{
    public class FirewallRuleExporter
    {
        public static async Task<int> ExportFirewallRules(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
        {
            try
            {
                Logger.LogInfo($"開始匯出防火牆規則 - 裝置: {device.DeviceName} ({device.DeviceIP})");

                var rules = await cmdbSource.DoFirewallRules(device);

                if (rules == null || rules.Count == 0)
                {
                    Logger.LogWarning($"未取得任何防火牆規則 - 裝置: {device.DeviceName}");
                    return 0;
                }

                Logger.LogInfo($"取得 {rules.Count} 筆防火牆規則");

                var savedCount = await cmdbSource.SaveFirewallRules(rules);

                Logger.LogInfo($"成功儲存 {savedCount} 筆防火牆規則至資料庫");

                return savedCount;
            }
            catch (Exception ex)
            {
                Logger.LogError($"匯出防火牆規則時發生錯誤 - 裝置: {device.DeviceName}", ex);
                throw;
            }
        }
    }
}
