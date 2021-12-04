using Robosen.Optimus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Optimus.UnitTests
{
    public class RobotConnectionTests
    {
        private const string MockDeviceName = "OP-M-02CM";
        private static readonly byte[] MockManufacturerData = new byte[] { 0x88, 0xa0, 0x3c, 0xa5, 0x51, 0x88, 0x36, 0x9f };

        [Fact]
        public async Task Can_Timeout_When_Connecting()
        {
            MockBluetooth bluetooth = new MockBluetooth();
            var connection = await RobotConnection.ConnectToFirst(bluetooth, TimeSpan.FromMilliseconds(10));
            Assert.Null(connection);
        }

        [Fact]
        public async Task Can_Connect_and_Disconnect()
        {
            MockBluetooth bluetooth = new MockBluetooth();
            var connectTask = RobotConnection.ConnectToFirst(bluetooth, TimeSpan.FromSeconds(10));
            
            var device = new MockBluetoothDevice(MockDeviceName, MockManufacturerData);
            bluetooth.ActiveScan!.AdvertiseDevice(device);
            var connection = await connectTask;

            Assert.Null(bluetooth.ActiveScan);
            Assert.Equal(device.Name, connection!.Name);
            Assert.True(device.IsConnected);
            Assert.NotNull(device.ActiveConnection);

            connection.Dispose();
            Assert.False(device.IsConnected);
            Assert.Null(device.ActiveConnection);
        }
    }
}
