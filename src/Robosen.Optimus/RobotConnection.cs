using Robosen.Optimus.Bluetooth;
using Robosen.Optimus.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Robosen.Optimus
{
    internal class RobotConnection : IDisposable
    {
        private const ushort RobosenManufacturerId = 0x15b1;
        private const ushort RobotGattServiceId = 0xFFE0;
        private const ushort RobotGattCharacteristicId = 0xFFE1;

        private IBluetoothConnection connection;
        private Command activeCommand = null;

        internal RobotConnection(string name, IBluetoothConnection connection)
        {
            Name = name;
            this.connection = connection;
            connection.RecieveDataCallback = RecieveData;
        }

        public string Name { get; }

        public Action<DataPacket>? OnOutOfBandNotification { get; set; }

        private void RecieveData(byte[] data)
        {
            var response = new DataPacket(data);
            // TODO: should validate the response here 

            if (activeCommand != null)
            {
                if (activeCommand.RecieveData(response))
                {
                    // the command is complete
                    activeCommand = null;
                }
            }
            else if (OnOutOfBandNotification != null)
            {
                OnOutOfBandNotification(response);
            }
        }

        public async Task SendWithoutResponseAsync(DataPacket data)
        {
            if (!data.IsValid())
                throw new ArgumentException("The Data packet is invalid", nameof(data));

            await connection.SendData(data.Data);
        }

        public async Task<DataPacket> SendWithResponseAsync(DataPacket data, CommandType responseType)
        {
            if (!data.IsValid())
                throw new ArgumentException("The Data packet is invalid", nameof(data));

            var channel = await SendWithResponsesAsync(data, responseType);
            var response = await channel.ReadAsync();
            
            // TODO: is this the right thing to do here?
            if (response.CommandType != responseType)
                throw new InvalidOperationException($"An unexpected response of type {response.CommandType} was recieved when {responseType} was expected");

            return response;
        }

        public async Task<ChannelReader<DataPacket>> SendWithResponsesAsync(DataPacket data, CommandType responseType)
        {
            if (!data.IsValid())
                throw new ArgumentException("The Data packet is invalid", nameof(data));

            // TODO: what to do when there is already an active command
            if (activeCommand != null)
                throw new InvalidOperationException("There is already an active command that has not completed");

            activeCommand = new Command(responseType);
            await connection.SendData(data.Data);

            return activeCommand.Reader;
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        private class Command
        {
            private readonly CommandType responseType;
            private readonly Channel<DataPacket> channel;

            public Command (CommandType responseType)
            {
                this.responseType = responseType;
                channel = Channel.CreateUnbounded<DataPacket>();
            }

            internal bool RecieveData(DataPacket response)
            {
                // TODO: does this always succeed because it is unbounded?
                channel.Writer.TryWrite(response);

                if (response.CommandType == responseType)
                {
                    channel.Writer.Complete();
                    return true;
                }
                return false;
            }

            public ChannelReader<DataPacket> Reader => channel.Reader;
        }


        #region Static factory methods

        public static async Task<RobotConnection?> ConnectToFirst(IBluetooth bluetooth, TimeSpan timeout)
        {
            if (bluetooth is null)
                throw new ArgumentNullException(nameof(bluetooth));

            var deviceSource = new TaskCompletionSource<IBluetoothDevice>();
            using (var scan = await bluetooth.BeginDeviceScanAsync(RobosenManufacturerId, dev => deviceSource.SetResult(dev)))
            {
                await Task.WhenAny(deviceSource.Task, Task.Delay(timeout));
                // if we timeout return null
                if (!deviceSource.Task.IsCompleted)
                    return null;
            }

            var connection = await deviceSource.Task.Result.ConnectAsync(RobotGattServiceId, RobotGattCharacteristicId);

            return new RobotConnection(deviceSource.Task.Result.Name, connection);
        }

        #endregion
    }
}
