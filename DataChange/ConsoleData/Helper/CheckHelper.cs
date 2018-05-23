using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleData.Helper
{
    public static class CheckHelper
    {
        #region 辅助方法

        /// <summary>
        /// 校验数据是否合法
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static bool CheckData(byte[] buffer)
        {
            Console.WriteLine();
            StringBuilder sb = new StringBuilder();
            byte SOF = buffer[0];
            //length 是出去SOF 和EOF 的总长度
            int length = Convert.ToInt32(buffer[1]);
            byte EOF = buffer[length + 1];
            //判断结束位
            if (EOF != 0x3F)
            {
                return false;
            }
            byte[] sourceCRC = new byte[2];
            //获取CRC 字节
            Buffer.BlockCopy(buffer, length - 1, sourceCRC, 0, 2);
            //判断CRC ,长度为出去 BOF 和 EOF ，CRC
            byte[] CRCbyte = new byte[length - 2];
            Buffer.BlockCopy(buffer, 1, CRCbyte, 0, length - 2);

            //转出的CRC 始终是低位到高位，于C代码中高位到低位相反
            var convertCRC = BitConverter.GetBytes(CRC.CCITT_CRC16(CRCbyte));
            if (convertCRC[1] == sourceCRC[0] && convertCRC[0] == sourceCRC[1])
            {
                return true;
            }
            return false;
        }

        public static  byte[] ConvertByte(int c)
        {
            byte[] arry = new byte[4];
            arry[0] = (byte)(c & 0xFF);
            arry[1] = (byte)((c & 0xFF00) >> 8);
            arry[2] = (byte)((c & 0xFF0000) >> 16);
            arry[3] = (byte)((c >> 24) & 0xFF);

            return arry;
        }

        /// <summary>
        /// 判断是否所有的字符串都有 0x00 组成
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static bool CheckZero(byte[] buffer)
        {
            return Array.FindIndex(buffer, NotZero) < 0;
        }
        /// <summary>
        /// Array.FindIndex用谓词定义:大于0
        /// </summary>
        private static bool NotZero(byte d)
        {
            if (d != 0)
                return true;
            else
                return false;
        }
        #endregion
    }
}
