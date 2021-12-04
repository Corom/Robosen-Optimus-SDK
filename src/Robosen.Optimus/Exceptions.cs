using Robosen.Optimus.Protocol;

namespace Robosen.Optimus
{
    [Serializable]
    public class RobotException : Exception
    {
        public RobotException(string message) : base(message) { }
        public RobotException(string message, Exception inner) : base(message, inner) { }
        protected RobotException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class OverlappingCommandException : RobotException
    {
        public OverlappingCommandException(CommandType activeCommand)
            : base($"Cannot perform the command because an existing command of type {activeCommand} is in progress")
        {
            ActiveCommand = activeCommand;
        }

        protected OverlappingCommandException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public CommandType ActiveCommand { get; }
    }

    [Serializable]
    public class UnexpectedResponseException : RobotException
    {
        public UnexpectedResponseException(CommandType expectedType, DataPacket actualReponse)
            : base($"An unexpeted reponse of type {actualReponse.CommandType} was recieved when {expectedType} was expected.")
        {
            ExpectedType = expectedType;
            ActualReponse = actualReponse;
        }

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected UnexpectedResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        public CommandType ExpectedType { get; }
        public DataPacket ActualReponse { get; }
    }


    [Serializable]
    public class InvalidDataPacketException : RobotException
    {
        public InvalidDataPacketException(string message) : base(message) { }

        protected InvalidDataPacketException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
