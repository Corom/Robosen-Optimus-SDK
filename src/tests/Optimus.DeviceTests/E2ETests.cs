using Robosen.Optimus;
using Robosen.Optimus.Bluetooth;
using Robosen.Optimus.Protocol;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Optimus.DeviceTests
{
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public class E2ETests : IDisposable
    {
        private readonly RobotConnection robot;

        public E2ETests()
        {
            Trace.WriteLine("Looking for Optimus Prime.");
            var optimus = RobotConnection.ConnectToFirst(new BluetoothImplementation(), TimeSpan.FromSeconds(30)).Result;

            robot = optimus ?? throw new Exception("Optimus Prime was not found.");
        }

        public void Dispose() => robot?.Dispose();


        [Fact]
        public async Task TestDevice()
        {
            // handshake
            await robot.SendDataAsync(new DataPacket("ffff020b0d"));

            await Task.Delay(2000);

            // GetUserActionName 
            await robot.SendDataAsync(new DataPacket("ffff021012"));

            await Task.Delay(2000);

        }
    }
}