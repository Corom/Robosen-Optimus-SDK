using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Robosen.Optimus.Protocol
{
    /************************* Data packet structure  ***************************
     |  header | num bytes | command type |         data          | CheckSum |
     |---------|-----------|--------------|-----------------------|----------|
     |  ffff   |    02     |      0f      |                       |    11    |
     |---------|-----------|--------------|-----------------------|----------|
     |  ffff   |    08     |      19      | 32322f313230 "22/120" |    47    |
     |---------|-----------|--------------|-----------------------|----------|
     CheckSum = Sum of Bytes excluding header % 256
     ****************************************************************************/

    public struct DataPacket
    {
        private readonly byte[] data;

        public DataPacket(byte[] data)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public DataPacket(CommandType commandType, byte[]? commandData = null)
        {
            data = PackData(commandType, commandData);
        }

        // for use by tests
        internal DataPacket(string hex)
            : this(StringToByteArray(hex))
        {
        }

        public string ReadAsString()
        {
            EnsureIsValid();
            return Encoding.ASCII.GetString(Payload);
        }

        public bool ReadAsBoolean()
        {
            EnsureIsValid();
            if (PayloadLength != 1)
                throw new InvalidDataPacketException($"Incorrect number of bytes in Packet Payload for Boolean. Expected: 1, Acual {PayloadLength}");
            return BitConverter.ToBoolean(Payload);
        }

        public byte ReadAsByte()
        {
            EnsureIsValid();
            if (PayloadLength != 1)
                throw new InvalidDataPacketException($"Incorrect number of bytes in Packet Payload for Byte. Expected: 1, Acual {PayloadLength}");
            return Payload[0];
        }

        public T ReadAs<T>() where T : struct
        {
            T str = default(T);
            int size = Marshal.SizeOf(str);
            if (PayloadLength != size)
                throw new InvalidDataPacketException($"Incorrect number of bytes in Packet Payload for Byte. Expected: {size}, Acual {PayloadLength}");

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, PayloadStart, ptr, PayloadLength);
            str = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);

            return str;
        }

        public byte[] Data => data;

        public bool IsValid() => EnsureIsValid(throws: false);

        public void EnsureIsValid() => EnsureIsValid(throws: true);

        private bool EnsureIsValid(bool throws)
        {
            if (data.Length < 5)
                return throws ? throw new InvalidDataPacketException("The data packet is too small") : false;

            if (Header.Any(b => b != 0xFF))
                return throws ? throw new InvalidDataPacketException("The data packet header is invalid") : false;

            if (CommandLength != data.Length - 3)
                return throws ? throw new InvalidDataPacketException("The data packet command length is incorrect") : false;
            
            if (Checksum != CalulateCheckSum(data))
                return throws ? throw new InvalidDataPacketException("The data packet checksum is invalid") : false;
            
            return true;
        }

        public IEnumerable<byte> Header => data.Take(2);
        public int CommandLength => data[2];
        public CommandType CommandType => (CommandType)data[3];
        public ReadOnlySpan<byte> Payload => new ReadOnlySpan<byte>(data, PayloadStart, PayloadLength);
        public byte Checksum => data[data.Length-1];

        private int PayloadStart = 4;
        private int PayloadLength => data.Length - 5;

        private static byte[] PackData(CommandType commandType, byte[]? commandData)
        {
            byte[] data = new byte[(commandData?.Length ?? 0) + 5];
            data[0] = 0xFF;
            data[1] = 0xFF;
            data[2] = (byte)commandType;
            data[3] = (byte)(data.Length - 3);
            if (commandData != null)
            {
                commandData.CopyTo(data, 4);
            }
            data[data.Length - 1] = CalulateCheckSum(data);
            return data;
        }

        private static byte CalulateCheckSum(byte[] data)
        {
            byte checksum = 0;
            foreach (byte value in data.Skip(2).SkipLast(1))
            {
                checksum += value;
            }
            return checksum;
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }
        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public override string ToString()
        {
            return data != null ? BitConverter.ToString(data).Replace("-", "") : string.Empty;
        }

        #region operator overloading

        public override bool Equals(object? obj) => Data.Equals(obj as byte[] ?? ((obj is DataPacket) ? ((DataPacket)obj).Data : null));

        public override int GetHashCode() => Data.GetHashCode();

        public static implicit operator byte[](DataPacket packet) => packet.Data;

        public static explicit operator DataPacket(byte[] data) => new DataPacket(data);

        public static implicit operator string(DataPacket packet) => packet.ToString();

        public static implicit operator DataPacket(string data) => new DataPacket(data);

        #endregion
    }
}
