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
    public class OptimusPrimeTests
    {
        private const string MockDeviceName = "OP-M-02CM";
        private static readonly byte[] MockManufacturerData = new byte[] { 0x88, 0xa0, 0x3c, 0xa5, 0x51, 0x88, 0x36, 0x9f };

        [Fact]
        public async Task Can_Timeout_When_Connecting()
        {
            MockBluetooth bluetooth = new MockBluetooth();
            var robot = await OptimusPrime.ConnectToFirst(bluetooth, TimeSpan.FromMilliseconds(10));
            Assert.Null(robot);
        }

        [Fact]
        public async Task Can_Connect()
        {
            MockBluetooth bluetooth = new MockBluetooth();
            var connectTask = OptimusPrime.ConnectToFirst(bluetooth, TimeSpan.FromSeconds(10));
            
            var device = new MockBluetoothDevice(MockDeviceName, MockManufacturerData);
            bluetooth.ActiveScan!.AdvertiseDevice(device);
            var robot = await connectTask;

            Assert.Null(bluetooth.ActiveScan);
            Assert.Equal(device.Name, robot!.Name);
            Assert.True(device.IsConnected);
            Assert.NotNull(device.ActiveConnection);
        }
    }
}
