# Firewall Rule Export Console Application (DEMO)

## Overview
This is a demonstration console application designed to export firewall rules from various firewall brands to a database. The application uses a flexible architecture with interfaces to support multiple firewall vendors and CMDB systems.

## Features
- **Multi-vendor Support**: Extensible interface-based architecture supports different firewall brands
- **CMDB Integration**: Interface for integrating with different CMDB systems
- **ORM Framework**: Uses Dapper for efficient data access
- **Dependency Injection**: Supports DI for easy extension and testing
- **Comprehensive Logging**: Built-in logging system with file and console output
- **Group Expansion**: Automatically expands IP and service groups from both CMDB and firewall APIs

## Supported Firewall Vendors
1. **CheckPoint** (R80.x and above)
2. **FortiGate** (6.x and above)

## Architecture

### Interfaces
- `IFIREWALL_RULE_SET`: Firewall rule data interface
- `IFirewallAPIClient`: Firewall API client interface
- `ICMDBSource`: CMDB data source interface
- `IFirewallDeviceInfo`: Firewall device information interface
- `IApiUserInfo`: API user authentication information interface
- `IIPGroupInformation`: IP group information interface
- `IServiceInformation`: Service group information interface
- `IFirewallAPI`: Firewall API information interface

### Database Tables
1. **FIREWALL_RULE_SET**: Stores firewall rules
2. **FW_HW_INFO**: Firewall hardware information
3. **FW_API_INFO**: Firewall API configuration
4. **IP_GROUP_MAIN**: IP group main table
5. **IP_GROUP_DETAILS**: IP group details
6. **SERVICE_GROUP_MAIN**: Service group main table
7. **SERVICE_GROUP_DETAILS**: Service group details

## Installation & Setup

### Prerequisites
- .NET Framework 4.7.2 or higher
- SQL Server 2012 or higher
- Visual Studio 2017 or higher (for development)

### Steps
1. **Create Database**
   - Run the SQL script located at `Database\CreateDatabase.sql`
   - This will create the FirewallDB database and all required tables

2. **Configure Connection Strings**
   - Edit `App.config` file
   - Update the connection strings to point to your SQL Server instance
   ```xml
   <connectionStrings>
     <add name="DatabaseConnection" connectionString="Data Source=YOUR_SERVER;Initial Catalog=FirewallDB;Integrated Security=True" />
     <add name="CMDBConnection" connectionString="Data Source=YOUR_SERVER;Initial Catalog=CMDB;Integrated Security=True" />
   </connectionStrings>
   ```

3. **Install NuGet Packages**
   - Restore NuGet packages in Visual Studio
   - Or use: `nuget restore`

4. **Build the Solution**
   - Build the solution in Visual Studio
   - Or use: `msbuild FirewallRuleExportDemo.sln`

5. **Run the Application**
   - Execute `FirewallRuleExportDemo.exe`
   - The application will process all firewall devices configured in the database

## Configuration

### App.config Settings
- `APITimeout`: API connection timeout in milliseconds (default: 30000)
- `MaxRetryCount`: Maximum retry attempts for API calls (default: 3)
- `RetryDelayMilliseconds`: Delay between retry attempts (default: 1000)
- `UseProxy`: Enable/disable proxy server (default: false)
- `LogFilePath`: Log file path (default: Logs\FirewallExport.log)
- `LogLevel`: Logging level (Info, Debug, Trace)
- `EnableConsoleLog`: Enable console logging (default: true)
- `EnableFileLog`: Enable file logging (default: true)

## Extending the Application

### Adding a New Firewall Vendor
1. Create a new class implementing `IFirewallAPIClient`
2. Implement the `GetFirewallRules()` method
3. Register the new implementation in `DemoCMDBSource.DoFirewallRules()`

Example:
```csharp
public class PaloAltoFirewallAPIClient : IFirewallAPIClient
{
    public async Task<List<IFIREWALL_RULE_SET>> GetFirewallRules()
    {
        // Your implementation here
    }
    
    public IFirewallAPIClient GetClientInfo(IFirewallDeviceInfo device)
    {
        return new PaloAltoFirewallAPIClient(device, _cmdbSource);
    }
}
```

### Creating a Custom CMDB Source
1. Create a new class implementing `ICMDBSource`
2. Implement all required methods
3. Register your implementation in `Program.ConfigureServices()`

## API Integration

### CheckPoint API
- Requires API login session
- Uses web_api endpoints
- Supports access rule retrieval with hit counts
- Documentation: https://sc1.checkpoint.com/documents/latest/APIs/

### FortiGate API
- Uses API token authentication
- REST API v2 endpoints
- Supports policy retrieval with statistics
- Documentation: https://docs.fortinet.com/document/fortigate/7.0.0/cookbook/763437/rest-api

## Rule Expansion Logic

The application expands grouped objects (IP groups and service groups) into individual rules:

1. **IP Group Expansion**:
   - Checks if group exists in CMDB
   - If in CMDB: keeps group reference (not expanded)
   - If not in CMDB: retrieves from firewall API and expands

2. **Service Group Expansion**:
   - Same logic as IP group expansion
   - Supports TCP, UDP, and other protocols

3. **Rule Multiplication**:
   - Creates individual records for each combination
   - Source × Destination × Service = Total Rules

## Logging

The application uses a custom logging system:
- **Info**: General information messages
- **Warning**: Warning messages
- **Error**: Error messages with exception details
- **Debug**: Detailed debugging information
- **Trace**: Very detailed trace information

Logs are written to both console and file (configurable).

## Demo Data

The application includes demo data that will be used if no devices are found in the database:
- 2 sample firewall devices (CheckPoint and FortiGate)
- Sample IP groups
- Sample service groups
- Sample firewall rules

## Troubleshooting

### Common Issues

1. **Database Connection Error**
   - Verify SQL Server is running
   - Check connection string in App.config
   - Ensure database exists (run CreateDatabase.sql)

2. **API Timeout**
   - Increase APITimeout value in App.config
   - Check network connectivity to firewall
   - Verify firewall API is accessible

3. **No Rules Retrieved**
   - Check API credentials
   - Verify firewall API permissions
   - Review log files for detailed error messages

## Contact Information

**Author**: Edward Huang (黃建豪)  
**Email**: edward.huang@kli.com.tw  
**Company**: 寬聯資訊股份有限公司 (Kuan Lian Information Co., Ltd.)

## License

This is a demonstration application. Please contact the author for licensing information.

## Version History

- **v1.0.0** (2024): Initial DEMO release
  - CheckPoint support
  - FortiGate support
  - Basic rule expansion
  - Database integration
