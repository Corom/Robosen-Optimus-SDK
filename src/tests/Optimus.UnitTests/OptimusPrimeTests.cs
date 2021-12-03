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
            MockBluetooth bt = new MockBluetooth();
            Stopwatch sw = Stopwatch.StartNew();
            var robot = await OptimusPrime.ConnectToFirst(bt, TimeSpan.FromMilliseconds(200));
            sw.Stop();
            Assert.Null(robot);
            Assert.True(sw.ElapsedMilliseconds > 190 && sw.ElapsedMilliseconds < 250);
        }

        [Fact]
        public async Task Can_Connect()
        {
            MockBluetooth bt = new MockBluetooth();
            var connectTask = OptimusPrime.ConnectToFirst(bt, TimeSpan.FromSeconds(10));
            
            var device = new MockBluetoothDevice(MockDeviceName, MockManufacturerData);
            bt.ActiveScan!.AdvertiseDevice(device);
            var robot = await connectTask;

            Assert.Null(bt.ActiveScan);
            Assert.Equal(device.Name, robot!.Name);
            Assert.True(device.IsConnected);
            Assert.NotNull(device.ActiveConnection);
        }
    }
}
