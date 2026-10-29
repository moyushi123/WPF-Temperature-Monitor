# WPFTemperatureMonitor

基于 C#、.NET 8 和 WPF 的简易 Modbus TCP 温度监控学习项目。通过 XAML、数据绑定和命令实现温度显示、单次读取、自动采集及界面日志，用于练习 WPF 与基础 MVVM 思路。

这是温度监控项目的 WPF 简版，当前重点是通信与绑定流程，不是 WinForms 完整版的全部功能移植。

## 已实现功能

- 使用 NModbus 读取 Modbus TCP 设备的输入寄存器。
- 支持单次读取、开始自动采集和停止自动采集。
- 自动采集时每秒尝试读取一次，通信忙碌时跳过新请求。
- 显示最近一次成功读取的温度及状态。
- 以 `35℃` 为当前固定报警阈值，超限时显示红色状态文字。
- 显示最近 100 条界面日志，并自动滚动到最新记录。
- 使用后台任务执行同步通信，避免阻塞 UI。
- 处理连接失败和读写超时，并恢复按钮可用状态。
- 关闭窗口时停止定时器，避免已完成的后台读取继续更新界面。

## 技术栈

| 技术 | 用途 |
| --- | --- |
| C# / .NET 8 / WPF | 桌面应用 |
| XAML / Grid / StackPanel | 界面与布局 |
| DataContext / Binding | 连接界面和 ViewModel 数据 |
| INotifyPropertyChanged | 通知温度、状态属性变化 |
| ICommand / RelayCommand | 按钮命令与可执行状态管理 |
| ObservableCollection | 日志集合增删通知 |
| DispatcherTimer | 定时触发采集 |
| Task.Run / async / await | 后台通信及异步等待 |
| NModbus / TcpClient | Modbus TCP 通信 |

## 代码结构

| 文件 | 职责 |
| --- | --- |
| `App.xaml` / `App.xaml.cs` | 应用入口和启动窗口配置 |
| `MainWindow.xaml` | 温度、状态、按钮与日志界面 |
| `MainWindow.xaml.cs` | 设置 DataContext、日志滚动及窗口关闭处理 |
| `MainViewModel.cs` | 属性通知、采集流程、状态管理与命令 |
| `RelayCommand.cs` | 自定义 ICommand 实现 |
| `ModbusService.cs` | 连接设备、设置超时、读取及换算温度 |

界面通过绑定读取 ViewModel 属性，通过命令触发操作。日志滚动等纯界面行为保留在窗口代码中。当前规模较小，未额外引入 MVVM 框架或单独的数据模型层。

## 运行方法

1. 在 Windows 上准备支持 .NET 8 的 Visual Studio，安装“.NET 桌面开发”工作负载。
2. 下载或克隆仓库，打开项目中的 `.csproj` 文件（原项目名为 `WpfModbusMonitor.csproj`）。
3. 还原 NuGet 依赖：`NModbus 3.0.83`。
4. 单独启动已完成的 Modbus TCP 模拟设备程序，确认正在监听 `127.0.0.1:1502`。
5. 运行 WPF 项目，点击“读取一次”或“开始采集”。

仓库名与项目文件名可以不同，请以实际 `.csproj` 文件为准。模拟设备不会由上位机自动启动，也不能用仅收发自定义字节的普通 TCP 示例替代 Modbus TCP 设备。

### 当前通信参数

| 参数 | 当前值 |
| --- | --- |
| IP | `127.0.0.1` |
| 端口 | `1502` |
| 站号 / Unit ID | `1` |
| 功能码 | `0x04`：读取输入寄存器 |
| 起始地址 | `0` |
| 寄存器数量 | `1` |
| 温度换算 | 原始值除以 `10.0` |
| 连接及读写超时设置 | 各为 `2000ms` |
| 自动采集间隔 | `1` 秒 |

输入寄存器 `0` 的值为 `285` 时，界面显示 `28.5℃`。倍率由模拟设备约定，不是协议固定要求。

当前 IP、端口和寄存器参数写在 `ModbusService.cs` 中，尚未提供参数设置界面。`127.0.0.1` 表示当前电脑，因此模拟设备需要与上位机运行在同一台电脑上。

## 核心流程

1. 按钮通过 `Command` 绑定取得 ViewModel 的命令对象。
2. 命令调用开始、停止或单次读取方法。
3. 自动采集由 `DispatcherTimer` 触发，使用 `isBusy` 避免重叠通信。
4. `Task.Run` 在后台执行同步设备读取。
5. 等待结束后回到 UI 线程，更新温度、状态和日志集合。
6. `PropertyChanged` 通知属性变化；`ObservableCollection` 通知日志增删。
7. 状态变化时发出 `CanExecuteChanged`，由 WPF 重新判断按钮是否可用。

`async/await` 和命令绑定本身不会自动把同步通信移到后台；本项目通过 `Task.Run` 实现这一点。

## 异常与停止行为

- 连接失败或读取超时后，状态和日志显示错误。
- 失败不会将最近温度替换成零；显示值可能是上一轮成功采样，应结合当前状态判断。
- 自动采集开启时，后续定时触发会继续尝试通信。
- 停止采集只停止新请求，不强制取消正在进行的读取；本轮结果随后会被忽略。
- 旧读取未结束时暂不允许重新开始，防止请求重叠。
- 每次读取建立并释放连接，当前没有长连接重连管理。

## 发布

在 `.csproj` 所在目录执行：

```powershell
dotnet publish .\WpfModbusMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o ..\WPFTemperatureMonitor_Release
```

如果项目文件改过名称，请同步调整命令。发布后双击输出目录中的 EXE；发送给其他人时，需要保留整个发布目录。该方式自带 .NET 运行时，采集时仍需运行模拟设备或连接真实设备。

## 当前范围与后续计划

当前版本仅支持 Modbus TCP 温度读取和界面日志，尚未实现：

- Modbus RTU 通信。
- 目标温度写入与读回验证。
- 实时温度曲线。
- CSV 数据和日志文件保存。
- IP、端口等参数的界面设置与持久化。

这些功能可在后续逐步增加。项目基于教程与辅助指导完成，主要用于学习 WPF 数据绑定、命令和工业通信集成，尚未在真实生产现场验证。
