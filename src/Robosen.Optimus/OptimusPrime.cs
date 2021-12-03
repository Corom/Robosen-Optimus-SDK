using Robosen.Optimus.Bluetooth;

namespace Robosen.Optimus
{
    public class OptimusPrime : IDisposable
    {
        private const ushort RobosenManufacturerId = 0x15b1;
        private const ushort RobotGattServiceId = 0xFFE0;
        private const ushort RobotGattCharacteristicId = 0xFFE1;

        private readonly IBluetoothConnection connection;

        private OptimusPrime(string name, IBluetoothConnection connection)
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

        public async Task SendDataAsync(byte[] data)
        {
            await connection.SendData(data);
        }

        public void Dispose()
        {
            connection.Dispose();
        }


        #region Static Factory Members

        public static async Task<OptimusPrime?> ConnectToFirst(IBluetooth bluetooth, TimeSpan timeout)
        {
            if (bluetooth is null)
            {
                throw new ArgumentNullException(nameof(bluetooth));
            }

            var deviceSource = new TaskCompletionSource<IBluetoothDevice>();
            using (var scan = await bluetooth.BeginDeviceScanAsync(RobosenManufacturerId, dev => deviceSource.SetResult(dev)))
            {
                await Task.WhenAny(deviceSource.Task, Task.Delay(timeout));
                // if we timeout return null
                if (!deviceSource.Task.IsCompleted)
                    return null;
            }

            var connection = await deviceSource.Task.Result.ConnectAsync(RobotGattServiceId, RobotGattCharacteristicId);
            return new OptimusPrime(deviceSource.Task.Result.Name, connection);
        }

        #endregion

    }
}