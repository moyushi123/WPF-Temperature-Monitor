using System;
using System.Net.Sockets;
using System.Threading;
using NModbus;

namespace WpfModbusMonitor
{
    public class ModbusService
    {
        // 这是同步方法，稍后由Task.Run放到后台执行。
        public double ReadTemperature()
        {
            // using保证方法结束或发生异常时释放连接。
            using TcpClient client = new TcpClient();

            // 为建立TCP连接设置2秒的等待期限。
            using CancellationTokenSource timeout =
                new CancellationTokenSource(2000);

            try
            {
                // 在后台等待连接完成。
                // 这段写法先理解为“连接设备，超过期限就取消等待”。
                client.ConnectAsync(
                    "127.0.0.1", 1502, timeout.Token)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("TCP连接超时");
            }

            ModbusFactory factory = new ModbusFactory();

            using IModbusMaster master =
                factory.CreateMaster(client);

            // 连接成功后，读写报文也需要设置超时。
            master.Transport.ReadTimeout = 2000;
            master.Transport.WriteTimeout = 2000;
            master.Transport.Retries = 0;

            // 站号1，输入寄存器地址0，读取1个。
            ushort[] values =
                master.ReadInputRegisters(1, 0, 1);

            // 按模拟设备约定：285表示28.5℃。
            return values[0] / 10.0;
        }
    }
}