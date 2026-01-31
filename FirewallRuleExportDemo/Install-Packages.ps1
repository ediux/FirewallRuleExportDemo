# PowerShell Script to Install Required NuGet Packages
# 防火牆規則匯出專案 - NuGet 套件安裝腳本

Write-Host "========================================" -ForegroundColor Green
Write-Host "防火牆規則匯出專案 - NuGet 套件安裝" -ForegroundColor Green
Write-Host "Firewall Rule Export - NuGet Package Installation" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Check if NuGet is available
$nugetPath = Get-Command nuget -ErrorAction SilentlyContinue

if (-not $nugetPath) {
    Write-Host "找不到 NuGet 命令列工具。正在下載..." -ForegroundColor Yellow
    Write-Host "NuGet CLI not found. Downloading..." -ForegroundColor Yellow
    
    $nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
    $nugetExe = "$PSScriptRoot\nuget.exe"
    
    try {
        Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetExe
        Write-Host "NuGet 下載完成!" -ForegroundColor Green
        Write-Host "NuGet downloaded successfully!" -ForegroundColor Green
        $nugetPath = $nugetExe
    }
    catch {
        Write-Host "無法下載 NuGet。請手動從 https://www.nuget.org/downloads 下載" -ForegroundColor Red
        Write-Host "Failed to download NuGet. Please download manually from https://www.nuget.org/downloads" -ForegroundColor Red
        exit 1
    }
}

Write-Host "使用 NuGet: $($nugetPath.Source)" -ForegroundColor Cyan
Write-Host ""

# Define required packages
$packages = @(
    @{Name="Dapper"; Version="2.0.123"},
    @{Name="Microsoft.Extensions.DependencyInjection"; Version="6.0.0"},
    @{Name="Microsoft.Extensions.DependencyInjection.Abstractions"; Version="6.0.0"},
    @{Name="Newtonsoft.Json"; Version="13.0.3"},
    @{Name="System.Configuration.ConfigurationManager"; Version="6.0.0"}
)

$projectPath = "$PSScriptRoot"
$packagesFolder = "$projectPath\packages"

# Create packages folder if it doesn't exist
if (-not (Test-Path $packagesFolder)) {
    New-Item -ItemType Directory -Path $packagesFolder -Force | Out-Null
}

Write-Host "開始安裝 NuGet 套件..." -ForegroundColor Yellow
Write-Host "Starting NuGet package installation..." -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$failCount = 0

foreach ($package in $packages) {
    $packageName = $package.Name
    $packageVersion = $package.Version
    
    Write-Host "正在安裝 Installing: $packageName $packageVersion" -ForegroundColor White
    
    try {
        $command = "install $packageName -Version $packageVersion -OutputDirectory `"$packagesFolder`" -NonInteractive"
        
        if ($nugetPath.Source) {
            $result = & $nugetPath.Source $command.Split(' ')
        }
        else {
            $result = & nuget $command.Split(' ')
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ 安裝成功 Installation successful: $packageName" -ForegroundColor Green
            $successCount++
        }
        else {
            Write-Host "  ✗ 安裝失敗 Installation failed: $packageName" -ForegroundColor Red
            $failCount++
        }
    }
    catch {
        Write-Host "  ✗ 安裝失敗 Installation failed: $packageName" -ForegroundColor Red
        Write-Host "    錯誤 Error: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "安裝完成 Installation Complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "成功 Success: $successCount" -ForegroundColor Green
Write-Host "失敗 Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "所有套件安裝成功！您可以開始建置專案了。" -ForegroundColor Green
    Write-Host "All packages installed successfully! You can now build the project." -ForegroundColor Green
}
else {
    Write-Host "有 $failCount 個套件安裝失敗。請檢查錯誤訊息或手動安裝。" -ForegroundColor Yellow
    Write-Host "$failCount package(s) failed to install. Please check error messages or install manually." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "按任意鍵結束... Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
