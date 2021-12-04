using Robosen.Optimus;
using Robosen.Optimus.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Optimus.UnitTests
{
    public class RobotConnectionTests
    {
        private const string MockDeviceName = "OP-M-02CM";
        private static readonly byte[] MockManufacturerData = new byte[] { 0x88, 0xa0, 0x3c, 0xa5, 0x51, 0x88, 0x36, 0x9f };
        private static readonly TimeSpan testTimeout = TimeSpan.FromMilliseconds(100);

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

        [Fact]
        public async Task Can_SendData_Without_Response()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff020f11");
            
            await robot.SendWithoutResponseAsync(dataToSend).TimeoutAfter(testTimeout);
            Assert.Single(conn.DataSent);
            Assert.Equal(dataToSend, conn.DataSent.Peek());
            Assert.False(robot.CommandIsActive);
        }


        [Fact]
        public async Task Can_SendData_With_Single_Response()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff020b0d");
            var dataToRecieve = new DataPacket("ffff030b000e");

            var sendTask = robot.SendWithResponseAsync(dataToSend, dataToRecieve.CommandType);
            await Task.Yield();
            Assert.False(sendTask.IsCompleted);
            Assert.Single(conn.DataSent);
            Assert.Equal(dataToSend, conn.DataSent.Peek());
            Assert.True(robot.CommandIsActive);

            conn.Recieve(dataToRecieve);
            var response = await sendTask.TimeoutAfter(testTimeout);
            Assert.Equal(dataToRecieve, response);
            Assert.False(robot.CommandIsActive);
        }

        [Fact]
        public async Task Wrong_Response_For_Single_Throws_Exception()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff020b0d"); // handshake
            var expectedDataToRecieve = new DataPacket("ffff030b000e"); // handshake response
            var actualDataToRecieve = new DataPacket("ffff0c0f00458c00000000000000ec"); // states response

            var sendTask = robot.SendWithResponseAsync(dataToSend, expectedDataToRecieve.CommandType);
            conn.Recieve(actualDataToRecieve);
            var error = await Assert.ThrowsAsync<UnexpectedResponseException>(() => sendTask);
            Assert.Equal(expectedDataToRecieve.CommandType, error.ExpectedType);
            Assert.Equal(actualDataToRecieve, error.ActualReponse);
            Assert.False(robot.CommandIsActive);
        }

        [Fact]
        public async Task Can_SendData_With_Multiple_Responses()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff021012");
            var dataToRecieveFirst = new DataPacket("ffff030b000e");
            var dataToRecieveLast = new DataPacket("ffff03fafaf7");

            var reader = await robot.SendWithResponsesAsync(dataToSend, dataToRecieveLast.CommandType).TimeoutAfter(testTimeout);
            Assert.Single(conn.DataSent);
            Assert.Equal(dataToSend, conn.DataSent.Peek());
            Assert.False(reader.Completion.IsCompleted);
            Assert.True(robot.CommandIsActive);

            DataPacket? response;
            Assert.False(reader.TryRead(out response));

            conn.Recieve(dataToRecieveFirst);
            response = await reader.ReadAsync().TimeoutAfter(testTimeout);
            Assert.Equal(dataToRecieveFirst, response);
            Assert.False(reader.Completion.IsCompleted);
            Assert.True(robot.CommandIsActive);

            conn.Recieve(dataToRecieveLast);
            response = await reader.ReadAsync().TimeoutAfter(testTimeout);
            Assert.Equal(dataToRecieveLast, response);
            Assert.True(reader.Completion.IsCompleted);
            Assert.False(robot.CommandIsActive);
        }

        [Fact]
        public async Task Recieved_Packet_Without_Active_Command_Use_Callback()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff020b0d");
            var dataToRecieve = new DataPacket("ffff030b000e");
            var outOfBoundPacket1 = new DataPacket("ffff03fafaf7");
            var outOfBoundPacket2 = new DataPacket("ffff03e7e7d1");

            var outOfBoundData = new Queue<DataPacket>();
            robot.OutOfBandNotificationCallback = outOfBoundData.Enqueue;

            // recieve a packet before any command is run
            conn.Recieve(outOfBoundPacket1);
            Assert.Single(outOfBoundData);
            Assert.Equal(outOfBoundData.Dequeue(), outOfBoundPacket1);

            // recieve an extra packet after a command
            var sendTask = robot.SendWithResponseAsync(dataToSend, dataToRecieve.CommandType);
            conn.Recieve(dataToRecieve);
            conn.Recieve(outOfBoundPacket2);
            var response = await sendTask;
            Assert.Equal(dataToRecieve, response);
            Assert.Single(outOfBoundData);
            Assert.Equal(outOfBoundData.Dequeue(), outOfBoundPacket2);
        }

        [Fact]
        public async Task Cannot_Send_Invalid_Commands()
        {
            var (robot, conn) = await GetConnectionAsync();
            var invalidData = new DataPacket("ff3f020b00");

            await Assert.ThrowsAsync<InvalidDataPacketException>(() => robot.SendWithoutResponseAsync(invalidData));
            await Assert.ThrowsAsync<InvalidDataPacketException>(() => robot.SendWithResponseAsync(invalidData, CommandType.HandShake));
            await Assert.ThrowsAsync<InvalidDataPacketException>(() => robot.SendWithResponsesAsync(invalidData, CommandType.HandShake));
        }

        [Fact]
        public async Task Cannot_Send_Overlapped_Commands()
        {
            var (robot, conn) = await GetConnectionAsync();
            var dataToSend = new DataPacket("ffff020b0d");
            var sendTask = robot.SendWithResponseAsync(dataToSend, CommandType.HandShake);

            await Assert.ThrowsAsync<OverlappingCommandException>(() => robot.SendWithoutResponseAsync(dataToSend));
            await Assert.ThrowsAsync<OverlappingCommandException>(() => robot.SendWithResponseAsync(dataToSend, CommandType.HandShake));
            await Assert.ThrowsAsync<OverlappingCommandException>(() => robot.SendWithResponsesAsync(dataToSend, CommandType.HandShake));
        }


        private static async Task<(RobotConnection,MockBluetoothConnection)> GetConnectionAsync()
        {
            MockBluetooth bluetooth = new MockBluetooth();
            var connectTask = RobotConnection.ConnectToFirst(bluetooth, TimeSpan.FromSeconds(10));

            var device = new MockBluetoothDevice(MockDeviceName, MockManufacturerData);
            bluetooth.ActiveScan!.AdvertiseDevice(device);

            return ((await connectTask)!, device.ActiveConnection!);
        }

    }
}
