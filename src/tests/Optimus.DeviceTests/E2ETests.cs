using Robosen.Optimus;
using Robosen.Optimus.Bluetooth;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Optimus.DeviceTests
{
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public class E2ETests : IDisposable
    {
        private readonly OptimusPrime robot;

        public E2ETests()
        {
            Trace.WriteLine("Looking for Optimus Prime.");
            var optimus = OptimusPrime.ConnectToFirst(new BluetoothImplementation(), TimeSpan.FromSeconds(30)).Result;

            robot = optimus ?? throw new Exception("Optimus Prime was not found.");
        }

        public void Dispose() => robot?.Dispose();


        [Fact]
        public async Task TestDevice()
        {
            // handshake
            await robot.SendDataAsync(new byte[] { 0xff, 0xff, 0x02, 0x0b, 0x0d });

            await Task.Delay(2000);

            // handshake with bad checksum
            await robot.SendDataAsync(new byte[] { 0xff, 0xff, 0x02, 0x0b, 0x0f });

            await Task.Delay(2000);

            // GetUserActionName 
            await robot.SendDataAsync(new byte[] { 0xff, 0xff, 0x02, 0x10, 0x12 });

            await Task.Delay(2000);

        }
    }
}