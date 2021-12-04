namespace Robosen.Optimus.Bluetooth
{
    public interface IBluetoothDevice
    {
        byte[] ManufacturerData { get; }
        string Name { get; }
        bool IsConnected { get; }
        Task<IBluetoothConnection> ConnectAsync(ushort serviceId, ushort characteristicId);
    }
}
