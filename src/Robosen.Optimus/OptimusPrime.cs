using Robosen.Optimus.Bluetooth;

namespace Robosen.Optimus
{
    public class OptimusPrime : IDisposable
    {
        private readonly RobotConnection connection;

        private OptimusPrime(RobotConnection connection)
        {
            this.connection = connection;
        }

        public string Name => connection.Name;


        public void Dispose()
        {
            connection.Dispose();
        }


        #region Static Factory Members

        public static async Task<OptimusPrime?> ConnectToFirst(IBluetooth bluetooth, TimeSpan timeout)
        {
            if (bluetooth is null)
                throw new ArgumentNullException(nameof(bluetooth));

            var connection = await RobotConnection.ConnectToFirst(bluetooth, timeout);
            return connection != null ? new OptimusPrime(connection) : null;
        }

        #endregion

    }
}