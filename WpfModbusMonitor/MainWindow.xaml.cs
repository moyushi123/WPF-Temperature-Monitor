using System.Collections.Specialized;
using System.Windows;

namespace WpfModbusMonitor
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = viewModel;

            // 日志集合变化时，让列表滚动到最新记录。
            viewModel.Logs.CollectionChanged += Logs_CollectionChanged;
        }

        private void Logs_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (viewModel.Logs.Count > 0)
            {
                LogList.ScrollIntoView(
                    viewModel.Logs[viewModel.Logs.Count - 1]);
            }
        }
    }
}