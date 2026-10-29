using System;
using System.Windows.Input;

namespace WpfModbusMonitor
{
    // 将普通方法包装成WPF按钮可以使用的命令。
    // 这是可复用的小工具，以后的按钮也能使用它。
    public class RelayCommand : ICommand
    {
        // 保存“执行什么”的方法。
        private readonly Action execute;

        // 保存“是否允许执行”的判断方法，可以不提供。
        private readonly Func<bool>? canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            // 这里只保存方法，不立即调用。
            this.execute = execute;
            this.canExecute = canExecute;
        }

        // WPF通过这个方法判断按钮是否可以使用。
        public bool CanExecute(object? parameter)
        {
            // 没有提供判断方法，就默认允许执行。
            if (canExecute == null)
            {
                return true;
            }

            return canExecute();
        }

        // 点击按钮后，执行保存的方法。
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                execute();
            }
        }

        public event EventHandler? CanExecuteChanged;

        // 以后通信状态变化时，调用它通知按钮重新判断能否执行。
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}