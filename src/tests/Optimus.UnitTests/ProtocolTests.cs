using Robosen.Optimus;
using Robosen.Optimus.Protocol;
using Xunit;

namespace Optimus.UnitTests
{
    public class ProtocolTests
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
    }
}