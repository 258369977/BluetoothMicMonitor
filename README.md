# BluetoothMicMonitor
蓝牙耳机免提通道自动切换工具。当指定程序（如游戏、QQ、微信等）启动时自动禁用蓝牙耳机的 Hands-Free 通道，恢复高清音频；程序退出后自动恢复。

## 功能

- **自动切换**：目标进程启动 → 禁用免提通道 + 重启蓝牙服务；全部退出 → 恢复
- **系统托盘**：关闭窗口最小化到托盘，右键菜单可打开面板 / 查看日志 / 退出
- **开机自启**：通过 Windows 计划任务实现，登录时以最高权限静默启动（无托盘、无 UAC 弹窗）
- **自定义监控**：可配置耳机设备名称、目标进程列表
- **设备扫描**：一键扫描 MEDIA 类蓝牙音频设备
- **日志系统**：按日滚动记录到 `%LocalAppData%\BluetoothMicMonitor\logs\`

## 截图

> 运行后系统托盘会出现图标，双击打开设置面板。

<img width="817" height="709" alt="image" src="https://github.com/user-attachments/assets/7eb5bbb6-c5cb-4b07-aaac-fbd4cdedc6e8" >


## 构建

### 环境要求

- Windows 10 / 11
- .NET Framework 4.8（系统预装，无需额外安装）

### 编译

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

`build.ps1` 会调用 `csc.exe`（.NET Framework 4.8 编译器）编译全部 10 个 `.cs` 源文件，生成 `BluetoothMicMonitor.exe`。

### 手动编译

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe ^
  /target:winexe /langversion:5 /win32manifest:app.manifest ^
  /out:BluetoothMicMonitor.exe ^
  /reference:"%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll" ^
  /reference:"%SystemRoot%\Microsoft.NET\assembly\GAC_32\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll" ^
  /reference:"%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll" ^
  /reference:"%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll" ^
  /reference:System.dll /reference:System.Core.dll ^
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ^
  /reference:System.Runtime.Serialization.dll ^
  AppEvent.cs EventBus.cs Logger.cs ConfigManager.cs TrayService.cs ^
  ProcessWatcher.cs DeviceWorker.cs Program.cs MainWindow.cs SetupApi.cs
```

## 使用说明

### 首次运行

1. **以管理员身份运行** `BluetoothMicMonitor.exe`（必须，因为需要操作 PnP 设备和重启蓝牙服务）
2. 输入蓝牙耳机名称（可点击「扫描」自动搜索 MEDIA 类设备）
3. 添加需要监控的进程名（如 `QQ.exe`、`r5apex_dx12.exe`）
4. 点击「启动」开始监控
5. 点击「开机自启」安装计划任务，此后登录自动后台运行

### 命令行参数

| 参数 | 说明 |
|------|------|
| （无） | 正常启动，显示托盘和设置窗口 |
| `--minimized` | 静默模式，无托盘无窗口，仅后台守护 |

### 配置文件

配置存储在 `%LocalAppData%\BluetoothMicMonitor\config.json`：

```json
{
  "targetProcesses": ["QQ.exe", "r5apex_dx12.exe"],
  "deviceName": "ROSE EarFeel i7 Hands-Free",
  "autoStart": true
}
```

## 项目结构

```
BluetoothMicMonitor/
├── Program.cs          入口：单实例控制、管理员提权、启动模式分流
├── MainWindow.cs       WPF 设置窗口（纯代码构建，无 XAML）
├── ProcessWatcher.cs   进程监控（轮询模式，无外部依赖）
├── DeviceWorker.cs     PnP 设备控制 + 蓝牙服务重启
├── EventBus.cs         事件管道（ConcurrentQueue + SemaphoreSlim）
├── SetupApi.cs         SetupAPI P/Invoke（MEDIA class 过滤）
├── ConfigManager.cs    配置管理 + 计划任务开机自启
├── Logger.cs           日志系统
├── TrayService.cs      系统托盘图标与右键菜单
├── AppEvent.cs         事件数据模型
├── app.manifest        嵌入的 Win32 清单（asInvoker 提权策略）
└── build.ps1           构建脚本
```

## 技术要点

- **设备过滤**：SetupAPI 指定 `MEDIA` class GUID (`{4d36e96c-e325-11ce-bfc1-08002be10318}`) 精准定位蓝牙硬件，避开 `AudioEndpoint` 软件节点
- **C# 5 兼容**：代码兼容 .NET Framework 4.8 的 C# 5 编译器，无 NuGet 依赖
- **无托盘模式**：`--minimized` 启动时纯后台运行，使用命名 Mutex 实现单实例控制——手动启动时自动接管已有静默实例
- **开机自启**：借助 `schtasks /sc onlogon /rl highest` 创建计划任务，登录时以最高权限无 UAC 弹窗启动
