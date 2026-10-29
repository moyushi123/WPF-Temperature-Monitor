using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace WpfModbusMonitor
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // 通信对象，专门负责读取设备。
        private readonly ModbusService device = new ModbusService();

        private bool isBusy = false;

        private string temperatureDisplay = "当前温度：-- ℃";
        private string statusDisplay = "状态：等待读取";

        public string TemperatureDisplay
        {
            get { return temperatureDisplay; }
            set
            {
                temperatureDisplay = value;
                OnPropertyChanged(nameof(TemperatureDisplay));
            }
        }

        public string StatusDisplay
        {
            get { return statusDisplay; }
            set
            {
                statusDisplay = value;
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }

        public ObservableCollection<string> Logs { get; }
            = new ObservableCollection<string>();

        public RelayCommand ReadCommand { get; }

        public MainViewModel()
        {
            // 第一个方法：点击后执行什么。
            // 第二个方法：现在是否允许执行。
            ReadCommand = new RelayCommand(ReadOnce, CanRead);
        }

        private bool CanRead()
        {
            // 空闲时允许读取，忙碌时不允许。
            return !isBusy;
        }

        // 当前RelayCommand接收Action，因此这个命令入口使用async void。
        // 异步读取产生的异常必须在这里捕获。
        private async void ReadOnce()
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;

            // 通知按钮重新调用CanRead，此时返回false，按钮变灰。
            ReadCommand.RaiseCanExecuteChanged();

            try
            {
                StatusDisplay = "状态：正在读取设备";
                AddLog("开始读取：127.0.0.1:1502");

                // 只有设备通信放在后台。
                double temperature = await Task.Run(
                    device.ReadTemperature);

                // 从UI线程调用本方法，默认await后回到UI线程。
                // 因此在这里更新绑定属性和日志集合。
                TemperatureDisplay =
                    $"当前温度：{temperature:F1} ℃";

                StatusDisplay = temperature >= 35
                    ? "状态：温度超限"
                    : "状态：温度正常";

                AddLog($"读取成功：{temperature:F1} ℃");
            }
            catch (Exception ex)
            {
                // 超时、连接失败、Modbus异常等都显示在日志中。
                StatusDisplay = "状态：读取失败";

                AddLog($"读取失败：{ex.Message}");
            }
            finally
            {
                // 无论成功还是失败，都恢复读取按钮。
                isBusy = false;
                ReadCommand.RaiseCanExecuteChanged();
            }
        }

        private void AddLog(string message)
        {
            Logs.Add(
                $"{DateTime.Now:HH:mm:ss}  {message}");

            if (Logs.Count > 100)
            {
                Logs.RemoveAt(0);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}