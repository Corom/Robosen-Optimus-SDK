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
        private Command? activeCommand = null;

        internal RobotConnection(string name, IBluetoothConnection connection)
        {
            Name = name;
            this.connection = connection;
            connection.RecieveDataCallback = RecieveData;
        }

        public string Name { get; }

        public Action<DataPacket>? OutOfBandNotificationCallback { get; set; }

        public bool CommandIsActive => activeCommand != null;

        private void RecieveData(byte[] data)
        {
            var response = new DataPacket(data);

            if (activeCommand != null)
            {
                if (activeCommand.RecieveData(response))
                {
                    // the command is complete
                    activeCommand = null;
                }
            }
            else if (OutOfBandNotificationCallback != null)
            {
                OutOfBandNotificationCallback(response);
            }
        }

        public async Task SendWithoutResponseAsync(DataPacket data)
        {
            data.EnsureIsValid();

            if (activeCommand != null)
                throw new OverlappingCommandException(activeCommand.SentData.CommandType);

            await connection.SendData(data.Data);
        }

        public async Task<DataPacket> SendWithResponseAsync(DataPacket data, CommandType responseType)
        {
            var channel = await SendWithResponsesAsync(data, responseType, multipleResponses: false);
            try
            {
                return await channel.ReadAsync();
            }
            catch (ChannelClosedException e) when (e.InnerException is RobotException)
            {
                throw e.InnerException;
            }
        }

        public Task<ChannelReader<DataPacket>> SendWithResponsesAsync(DataPacket data, CommandType responseType)
        {
            return SendWithResponsesAsync(data, responseType, multipleResponses: true);
        }

        private async Task<ChannelReader<DataPacket>> SendWithResponsesAsync(DataPacket data, CommandType responseType, bool multipleResponses)
        {
            data.EnsureIsValid();

            if (activeCommand != null)
                throw new OverlappingCommandException(activeCommand.SentData.CommandType);

            activeCommand = new Command(data, responseType, multipleResponses);
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
            private readonly bool multipleResponses;
            private readonly Channel<DataPacket> channel;

            public Command (DataPacket sentData, CommandType responseType, bool multipleResponses)
            {
                SentData = sentData;
                this.responseType = responseType;
                this.multipleResponses = multipleResponses;
                channel = multipleResponses ? Channel.CreateUnbounded<DataPacket>() : Channel.CreateBounded<DataPacket>(1);
            }

            public ChannelReader<DataPacket> Reader => channel.Reader;

            public DataPacket SentData { get; }

            internal bool RecieveData(DataPacket response)
            {
                if (!multipleResponses && response.CommandType != responseType)
                {
                    channel.Writer.Complete(new UnexpectedResponseException(responseType, response));
                    return true;
                }

                channel.Writer.TryWrite(response);

                if (response.CommandType == responseType)
                {
                    channel.Writer.Complete();
                    return true;
                }
                
                return false;
            }
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
