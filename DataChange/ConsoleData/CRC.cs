using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleData
{
    public enum InitialCrcValue { Zeros, NonZero1 = 0xffff, NonZero2 = 0x1D0F }

    /// <summary>
    /// CRC校验，CCITT
    /// </summary>
    public class CRC
    {
        public static int GetCrc16(byte[] bytes)
        {
            ushort crc = 0x0000;
            ushort current;

            for (int i = 0; i < bytes.Length; i++)
            {
                current = bytes[i];
                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ current) & 0x0001) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ 0x8408);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                    current >>= 1;
                }
            }
            return crc;
        }


        public static ushort CCITT_CRC16(byte[] bytes)
        {
            ushort crc = 0x0000;
            for (int j = 0; j < bytes.Length; j++)
            {
                var current = bytes[j];
                crc = (ushort)(crc ^ current);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0x8408);
                    else
                        crc >>= 1;
                }
                current >>= 1;
            }
            return crc;
        }
    }
}
