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
}
