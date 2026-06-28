# BluetoothMicMonitor Build Script
# Compiles all 10 C# source files into BluetoothMicMonitor.exe
$dir = Join-Path $PSScriptRoot "."
$pf = 'C:\WINDOWS\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll'
$pc = 'C:\WINDOWS\Microsoft.NET\assembly\GAC_32\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'
$wb = 'C:\WINDOWS\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll'
$sx = 'C:\WINDOWS\Microsoft.NET\assembly\GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll'
$csc = 'C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

Write-Host "=== BluetoothMicMonitor Build ===" -ForegroundColor Cyan

# Fix MainWindow.cs Thickness (2-param -> 4-param) for .NET 4.8 compatibility
$mwFile = Join-Path $dir 'MainWindow.cs'
$mw = [System.IO.File]::ReadAllText($mwFile)
$mw = $mw.Replace('new Thickness(14, 0)', 'new Thickness(14, 0, 14, 0)')
$mw = $mw.Replace('new Thickness(6, 0)', 'new Thickness(6, 0, 6, 0)')
[System.IO.File]::WriteAllText($mwFile, $mw)

Push-Location $dir
$result = & $csc /target:winexe /langversion:5 /out:BluetoothMicMonitor.exe `
  /reference:$pf /reference:$pc /reference:$wb /reference:$sx `
  /reference:System.dll /reference:System.Core.dll `
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
  /reference:System.Runtime.Serialization.dll `
  AppEvent.cs EventBus.cs Logger.cs ConfigManager.cs TrayService.cs `
  ProcessWatcher.cs DeviceWorker.cs Program.cs MainWindow.cs SetupApi.cs 2>&1

if ($LASTEXITCODE -eq 0) {
    $exe = Get-Item BluetoothMicMonitor.exe
    Write-Host "BUILD SUCCEEDED: $($exe.Name) ($($exe.Length) bytes)" -ForegroundColor Green
    Write-Host "" -ForegroundColor White
    Write-Host "Run: right-click BluetoothMicMonitor.exe -> Run as Administrator" -ForegroundColor Cyan
} else {
    $errors = @($result | Where-Object { $_ -match 'error CS' }).Count
    Write-Host "FAILED: $errors errors" -ForegroundColor Red
    $result | Where-Object { $_ -match 'error CS' } | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}
Pop-Location
