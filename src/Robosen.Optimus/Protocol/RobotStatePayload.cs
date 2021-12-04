using System.Runtime.InteropServices;

namespace Robosen.Optimus.Protocol
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RobotStatePayload
    {
        public byte Pattern;
        public byte Battery;
        public byte Volume;
        public byte PlayProgress;
        public byte Gyros;
        public byte Speed;
        public byte Charg;
        public byte AutoOff;
        public byte AutoTurn;
        public byte AutoPose;
    }
}
