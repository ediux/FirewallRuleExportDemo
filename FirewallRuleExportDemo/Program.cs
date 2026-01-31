using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirewallRuleExportDemo.Implementation;
using FirewallRuleExportDemo.Interfaces;
using FirewallRuleExportDemo.Services;
using FirewallRuleExportDemo.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace FirewallRuleExportDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine("防火牆規則匯出主控台應用程式 (DEMO)");
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine();

                var serviceProvider = ConfigureServices();
                var cmdbSource = serviceProvider.GetService<ICMDBSource>();

                Logger.Initialize(
                    cmdbSource.GetLogFilePath(),
                    cmdbSource.GetLogLevel(),
                    cmdbSource.GetEnableConsoleLog(),
                    cmdbSource.GetEnableFileLog()
                );

                Logger.LogInfo("程式啟動");

                var devices = cmdbSource.GetFirewallList();

                if (devices == null || devices.Count == 0)
                {
                    Logger.LogWarning("未找到任何防火牆裝置");
                    Console.WriteLine("未找到任何防火牆裝置，將使用示範資料...");
                    devices = GetDemoDevices();
                }

                Logger.LogInfo($"找到 {devices.Count} 個防火牆裝置");

                foreach (var device in devices)
                {
                    Console.WriteLine();
                    Console.WriteLine($"處理防火牆: {device.DeviceName} ({device.Brand} {device.Model})");
                    Console.WriteLine($"IP位址: {device.DeviceIP}");
                    Console.WriteLine("-".PadRight(60, '-'));

                    RunFirewallRuleExport(device, cmdbSource).Wait();
                }

                Logger.LogInfo("程式執行完成");
                Console.WriteLine();
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine("所有防火牆規則匯出完成");
                Console.WriteLine("=".PadRight(60, '='));
            }
            catch (Exception ex)
            {
                Logger.LogError("程式執行時發生錯誤", ex);
                Console.WriteLine($"發生錯誤: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("請按任意鍵結束程式...");
            Console.ReadKey();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ICMDBSource, DemoCMDBSource>();

            return services.BuildServiceProvider();
        }

        private static async Task RunFirewallRuleExport(IFirewallDeviceInfo device, ICMDBSource cmdbSource)
        {
            try
            {
                var exportedCount = await FirewallRuleExporter.ExportFirewallRules(device, cmdbSource);

                Console.WriteLine($"成功匯出 {exportedCount} 筆防火牆規則");

                if (exportedCount > 0)
                {
                    cmdbSource.SendNotification(
                        $"防火牆規則匯出完成 - {device.DeviceName}",
                        $"已成功匯出 {exportedCount} 筆防火牆規則至資料庫"
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"處理防火牆 {device.DeviceName} 時發生錯誤", ex);
                Console.WriteLine($"錯誤: {ex.Message}");

                cmdbSource.SendNotification(
                    $"防火牆規則匯出失敗 - {device.DeviceName}",
                    $"錯誤訊息: {ex.Message}"
                );
            }
        }

        private static List<IFirewallDeviceInfo> GetDemoDevices()
        {
            return new List<IFirewallDeviceInfo>
            {
                new Models.FirewallDeviceInfo
                {
                    Brand = "CheckPoint",
                    Model = "R80.40",
                    FWVersion = "R80.40",
                    DeviceName = "CheckPoint-FW-01",
                    DeviceIP = "192.168.100.1",
                    Description = "CheckPoint防火牆示範裝置"
                },
                new Models.FirewallDeviceInfo
                {
                    Brand = "FortiGate",
                    Model = "FortiGate-600D",
                    FWVersion = "v7.0.0",
                    DeviceName = "FortiGate-FW-01",
                    DeviceIP = "192.168.100.2",
                    Description = "FortiGate防火牆示範裝置"
                }
            };
        }
    }
}
