using Robosen.Optimus;
using Robosen.Optimus.Protocol;
using Xunit;

namespace Optimus.UnitTests
{
    public class DataPacketTests
    {
        [Fact]
        public void DataPacket_Are_Validatable()
        {
            Assert.True(new DataPacket("ffff020f11").IsValid());
            Assert.True(new DataPacket("ffff081932322f31323047").IsValid());

            Assert.False(new DataPacket("ffff020f").IsValid()); // too short
            Assert.False(new DataPacket("ffff030f11").IsValid()); // wrong count
            Assert.False(new DataPacket("ff00020f11").IsValid()); // bad header
            Assert.False(new DataPacket("ffff081932322f31323048").IsValid()); // bad checksum

            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff020f").EnsureIsValid());
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff030f11").EnsureIsValid());
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ff00020f11").EnsureIsValid());
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff081932322f31323048").EnsureIsValid());
        }

        [Fact]
        public void Can_Read_DataPacket_with_a_String()
        {
            Assert.Equal(new DataPacket("ffff081932322f31313248"), new DataPacket(CommandType.PlayAudioInFolder, "22/112"));

            Assert.Equal("CALLING", new DataPacket("ffff091043414c4c494e4713").ReadAsString()); // GetUserActionName response
            Assert.Equal("", new DataPacket("ffff020f11").ReadAsString()); // handshake request - no payload 

            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff071043414c4c494e4713").ReadAsString()); // Invlid data count

        }

        [Fact]
        public void Can_Read_DataPacket_with_a_Boolean()
        {
            Assert.Equal(new DataPacket("ffff030b000e"), new DataPacket(CommandType.HandShake, false));
            Assert.Equal(new DataPacket("ffff030b010f"), new DataPacket(CommandType.HandShake, true));

            Assert.False(new DataPacket("ffff030b000e").ReadAsBoolean()); // handshake response
            Assert.True(new DataPacket("ffff030b010f").ReadAsBoolean()); // handshake response
            Assert.True(new DataPacket("ffff03090814").ReadAsBoolean()); // RegularMoves response - number higher than 1 still true

            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff020f11").ReadAsBoolean()); // handshake request - no payload
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff091043414c4c494e4713").ReadAsBoolean()); // GetUserActionName response - too many bytes
        }

        [Fact]
        public void Can_Read_DataPacket_with_a_Byte()
        {
            Assert.Equal(new DataPacket("ffff0317102a"), new DataPacket(CommandType.FolderActionNameMovesOrActionProgress, 16));

            Assert.Equal(16, new DataPacket("ffff0317102a").ReadAsByte()); // FolderActionNameMovesOrActionProgress response

            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff020f11").ReadAsByte()); // handshake request - no payload
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff091043414c4c494e4713").ReadAsByte()); // GetUserActionName response - too many bytes
        }

        [Fact]
        public void Can_Read_DataPacket_with_a_Struct()
        {
            var expectedState = new RobotStatePayload()
            {
                AutoOff = 1,
                AutoPose = 1,
                Pattern = 1,
                Battery = 78,
                Volume = 143,
            };

            Assert.Equal(new DataPacket("ffff0c0f014e8f00000000010001fb"), new DataPacket(CommandType.States, expectedState));

            var state = new DataPacket("ffff0c0f014e8f00000000010001fb").ReadAs<RobotStatePayload>();
            Assert.Equal(expectedState, state);

            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff020f11").ReadAsByte()); // handshake request - no payload
            Assert.Throws<InvalidDataPacketException>(() => new DataPacket("ffff091043414c4c494e4713").ReadAsByte()); // GetUserActionName response - too few bytes
        }

    }
}
