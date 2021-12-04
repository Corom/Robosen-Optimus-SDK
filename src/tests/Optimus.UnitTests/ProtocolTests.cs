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

            Assert.False(new DataPacket("ffff020f").IsValid());
            Assert.False(new DataPacket("ffff030f11").IsValid());
            Assert.False(new DataPacket("ff00020f11").IsValid());
            Assert.False(new DataPacket("ffff081932322f31323048").IsValid());
        }
    }
}