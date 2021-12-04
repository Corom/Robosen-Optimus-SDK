using System;
using System.Collections.Generic;
using System.Linq;
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

    public class DataPacket
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

        public byte[] Data => data;

        public bool IsValid()
        {
            if (data.Length < 5)
                return false;
            return Header.All(b => b == 0xFF)
                && CommandLength == data.Length - 3
                && Checksum == CalulateCheckSum(data);
        }

        public IEnumerable<byte> Header => data.Take(2);
        public int CommandLength => data[2];
        public CommandType CommandType => (CommandType)data[3];
        public IEnumerable<byte> CommandData => data.Skip(4).Take(data.Length - 5);
        public byte Checksum => data[data.Length-1];

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
    }
}
