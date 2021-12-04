namespace Robosen.Optimus.Bluetooth
{
    public interface IBluetoothConnection : IDisposable
    {
        Action<byte[]>? RecieveDataCallback { get; set; }
        Task SendData(byte[] data);
    }
}
