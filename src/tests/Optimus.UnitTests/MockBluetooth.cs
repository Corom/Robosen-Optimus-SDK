using Robosen.Optimus.Bluetooth;
using Robosen.Optimus.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimus.UnitTests
{
    internal class MockBluetooth : IBluetooth
    {
        public MockScan? ActiveScan { get; private set; }

        public async Task<IDisposable> BeginDeviceScanAsync(ushort manufacturerId, Action<IBluetoothDevice> callback)
        {
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));
            if (ActiveScan != null)
                throw new InvalidOperationException("A scan is already active");

            ActiveScan = new MockScan(callback, () => ActiveScan = null);
            await Task.Yield();
            return ActiveScan;
        }
    }

    internal class MockScan : IDisposable
    {
        private readonly Action<IBluetoothDevice> callback;
        private readonly Action disposeCallback;
        private bool isDisposed = false;

        public MockScan(Action<IBluetoothDevice> callback, Action disposeCallback)
        {
            this.callback = callback;
            this.disposeCallback = disposeCallback;
        }

        public async Task Start()
        {
            await Task.Yield();
        }

        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(MockScan));
            isDisposed = true;
            disposeCallback();
        }

        public void AdvertiseDevice(MockBluetoothDevice device)
        {
            callback(device);
        }
    }

    internal class MockBluetoothDevice : IBluetoothDevice
    {
        public MockBluetoothDevice(string name, byte[] manufacturerData)
        {
            Name = name;
            ManufacturerData = manufacturerData;
        }

        public byte[] ManufacturerData { get; set; }

        public string Name { get; set; }

        public bool IsConnected => ActiveConnection != null;

        public MockBluetoothConnection? ActiveConnection { get; internal set; }

        public async Task<IBluetoothConnection> ConnectAsync(ushort serviceId, ushort characteristicId)
        {
            if (IsConnected)
                throw new InvalidOperationException("You are already connected to the device");

            ActiveConnection = new MockBluetoothConnection(this);
            await Task.Yield();
            return ActiveConnection; 
        }
    }

    internal class MockBluetoothConnection : IBluetoothConnection
    {
        private readonly MockBluetoothDevice device;
        private bool isDisposed = false;

        public MockBluetoothConnection(MockBluetoothDevice device)
        {
            this.device = device;
        }

        public Queue<byte[]> DataSent { get; } = new Queue<byte[]>();

        public Action<byte[]>? RecieveDataCallback { get; set; }

        private void RecieveData(byte[] data)
        {
            if (RecieveDataCallback != null)
            {
                RecieveDataCallback(data);
            }
        }

        public void Recieve(DataPacket packet)
        {
            RecieveData(packet.Data);
        }

        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(MockBluetoothConnection));
            device.ActiveConnection = null;
        }

        public async Task SendData(byte[] data)
        {
            if (data is null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));

            if (isDisposed)
                throw new InvalidOperationException("You cannot call SendData after disposing of the connection");

            DataSent.Enqueue(data);
        }
    }
}
