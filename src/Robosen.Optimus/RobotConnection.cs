using Robosen.Optimus.Bluetooth;
using Robosen.Optimus.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robosen.Optimus
{
    internal class RobotConnection : IDisposable
    {
        private const ushort RobosenManufacturerId = 0x15b1;
        private const ushort RobotGattServiceId = 0xFFE0;
        private const ushort RobotGattCharacteristicId = 0xFFE1;

        private IBluetoothConnection connection;

        internal RobotConnection(string name, IBluetoothConnection connection)
        {
            Name = name;
            this.connection = connection;
            connection.RecieveDataCallback = RecieveData;
        }

        public string Name { get; }


        private void RecieveData(byte[] data)
        {
            Console.WriteLine($"{data.Length} = {string.Join(" ", data.Select(b => string.Format("{0:X2}", b)))}");
        }

        public async Task SendDataAsync(DataPacket data)
        {
            if (!data.IsValid())
                throw new ArgumentException("The Data packet is invalid", nameof(data));

            await connection.SendData(data.Data);
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        #region Static factory methods

        public static async Task<RobotConnection?> ConnectToFirst(IBluetooth bluetooth, TimeSpan timeout)
        {
            if (bluetooth is null)
                throw new ArgumentNullException(nameof(bluetooth));

            var deviceSource = new TaskCompletionSource<IBluetoothDevice>();
            using (var scan = await bluetooth.BeginDeviceScanAsync(RobosenManufacturerId, dev => deviceSource.SetResult(dev)))
            {
                await Task.WhenAny(deviceSource.Task, Task.Delay(timeout));
                // if we timeout return null
                if (!deviceSource.Task.IsCompleted)
                    return null;
            }

            var connection = await deviceSource.Task.Result.ConnectAsync(RobotGattServiceId, RobotGattCharacteristicId);

            return new RobotConnection(deviceSource.Task.Result.Name, connection);
        }

        #endregion
    }
}
