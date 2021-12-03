using Robosen.Optimus.Protocol;
using Xunit;

namespace Optimus.UnitTests
{
    public class ProtocolTests
    {
        [Fact]
        public void DataPacket_Are_Validatable()
        {
            Assert.True(new DataPacket(new byte[] { 0xff, 0xff, 0x02, 0x0f, 0x11 }).IsValid());
            Assert.True(new DataPacket(new byte[] { 0xff, 0xff, 0x08, 0x19, 0x32, 0x32, 0x2f, 0x31, 0x32, 0x30, 0x47 }).IsValid());

            Assert.False(new DataPacket(new byte[] { 0xff, 0xff, 0x02, 0x0f }).IsValid());
            Assert.False(new DataPacket(new byte[] { 0xff, 0xff, 0x03, 0x0f, 0x11 }).IsValid());
            Assert.False(new DataPacket(new byte[] { 0xff, 0x00, 0x02, 0x0f, 0x11 }).IsValid());
            Assert.False(new DataPacket(new byte[] { 0xff, 0xff, 0x08, 0x19, 0x32, 0x32, 0x2f, 0x31, 0x32, 0x30, 0x48 }).IsValid());
        }
    }
}