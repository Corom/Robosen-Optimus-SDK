using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robosen.Optimus.Bluetooth
{
    public interface IBluetooth
    {
        Task<IDisposable> BeginDeviceScanAsync(ushort manufacturerId, Action<IBluetoothDevice> callback);
    }

    public interface IBluetoothDevice
    {
        byte[] ManufacturerData { get; }
        string Name { get; }
        bool IsConnected { get; }
        Task<IBluetoothConnection> ConnectAsync(ushort serviceId, ushort characteristicId);
    }

    public interface IBluetoothConnection : IDisposable
    {
        Action<byte[]>? RecieveDataCallback { get; set; }
        Task SendData(byte[] data);
    }
}
