using Ith=InTheHand.Bluetooth;
using InTheHand.Bluetooth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robosen.Optimus.Bluetooth
{
    public class BluetoothImplementation : IBluetooth
    {
        public async Task<IDisposable> BeginDeviceScanAsync(ushort manufacturerId, Action<IBluetoothDevice> callback)
        {
            var scan = new Scan(manufacturerId, callback);
            await scan.Start();
            return scan;
        }

        private class Scan : IDisposable
        {
            private readonly ushort manufacturerId;
            private readonly Action<IBluetoothDevice> callback;
            private BluetoothLEScan? scan = null;

            public Scan(ushort manufacturerId, Action<IBluetoothDevice> callback)
            {
                this.manufacturerId = manufacturerId;
                this.callback = callback;

                Ith.Bluetooth.AdvertisementReceived += AdvertisementReceived;
            }

            public async Task Start()
            {
                scan = await Ith.Bluetooth.RequestLEScanAsync(new BluetoothLEScanOptions());
            }

            public void Dispose()
            {
                if (scan != null)
                {
                    Ith.Bluetooth.AdvertisementReceived -= AdvertisementReceived;
                    scan.Stop();
                    scan = null;
                }
            }

            private void AdvertisementReceived(object? sender, BluetoothAdvertisingEvent e)
            {
                if (e?.ManufacturerData?.ContainsKey(manufacturerId) == true)
                {
                    var device = new BluetoothDeviceImpl(e.ManufacturerData[manufacturerId], e.Device);
                    callback(device);
                }
            }
        }

        private class BluetoothDeviceImpl : IBluetoothDevice
        {
            private readonly BluetoothDevice device;

            public BluetoothDeviceImpl(byte[] manufacturerData, BluetoothDevice device)
            {
                ManufacturerData = manufacturerData;
                this.device = device;
            }

            public byte[] ManufacturerData { get; }

            public string Name => device.Name;

            public bool IsConnected => device.Gatt.IsConnected;

            public async Task<IBluetoothConnection> ConnectAsync(ushort serviceId, ushort characteristicId)
            {
                if (IsConnected)
                    throw new InvalidOperationException("You are already connected to the device");

                await device.Gatt.ConnectAsync();
                var service = await device.Gatt.GetPrimaryServiceAsync(serviceId);
                var characteristic = await service.GetCharacteristicAsync(characteristicId);
                await characteristic.StartNotificationsAsync();
                return new BluetoothConnectionImpl(device, characteristic);
            }

        }

        private class BluetoothConnectionImpl : IBluetoothConnection
        {
            private readonly BluetoothDevice device;
            private readonly GattCharacteristic characteristic;
            private bool isDisposed = false;

            public BluetoothConnectionImpl(BluetoothDevice device, GattCharacteristic characteristic)
            {
                this.device = device;
                this.characteristic = characteristic;
                characteristic.CharacteristicValueChanged += CharacteristicValueChanged;
            }

            public Action<byte[]>? RecieveDataCallback { get; set; }

            private void CharacteristicValueChanged(object? sender, GattCharacteristicValueChangedEventArgs e)
            {
                if (RecieveDataCallback != null)
                {
                    RecieveDataCallback(e.Value);
                }
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    RecieveDataCallback = null; // ensure we dont leak
                    characteristic.CharacteristicValueChanged -= CharacteristicValueChanged;
                    device.Gatt.Disconnect();
                }
            }

            public async Task SendData(byte[] data)
            {
                if (data is null || data.Length == 0)
                    throw new ArgumentNullException(nameof(data));

                if (isDisposed)
                    throw new InvalidOperationException("You cannot call SendData after disposing of the connection");

                await characteristic.WriteValueWithResponseAsync(data);
            }
        }
    }
}
