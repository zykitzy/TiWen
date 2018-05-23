using Com.OCAMAR.Common.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleData.Helper
{
    public class OcaMarHelper
    {
        /// <summary>
        /// 得到昂科基站心跳，并且响应
        /// </summary>
        /// <param name="buffer">响应流</param>
        public static byte[] RecLocalTag(byte[] buffer)
        {
            if (buffer[2].ToString("X2") == "22")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString() + " ：");
                for (int i = 0; i <= 16; i++)
                {
                    sb.Append(buffer[i].ToString("X2"));
                    sb.Append(" ");
                }
                SysHelper.SetNews($"基站同步请求:{sb}");
                LogWriter.Info(sb.ToString());

                byte[] response = new byte[15];
                response[0] = 0x3E;
                response[1] = 0x0D;
                response[2] = 0xA2; //响应标准
                response[3] = buffer[3];
                response[4] = buffer[4];
                response[5] = buffer[5];
                response[6] = buffer[6];
                response[7] = buffer[7];
                response[8] = buffer[8];
                response[9] = buffer[9];
                response[10] = buffer[10];
                response[11] = buffer[11];
                response[12] = buffer[12];
                response[13] = buffer[13];
                response[14] = 0x3F;
                return response;
            }
            return null;
        }

        /// <summary>
        /// 发送事件片数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="callNumber"></param>
        /// <returns></returns>
        public static byte[] GetTagID(byte[] buffer, int callNumber)
        {
            byte[] command = new byte[16];
            command[0] = 0x3E;
            command[1] = 0x0c + 1;
            command[2] = 0xA9;
            //复制基站ID 和 标签ID
            for (int i = 3; i <= 10; i++)
            {
                command[i] = buffer[i];
            }
            command[11] = 0x30;
            command[12] = (byte)callNumber;
            byte[] crc = new byte[command.Length - 4];
            Buffer.BlockCopy(command, 1, crc, 0, command.Length - 4);
            byte[] convertCrc = BitConverter.GetBytes(CRC.CCITT_CRC16(crc));
            //颠倒高低位
            command[13] = convertCrc[1];
            command[14] = convertCrc[0];
            command[15] = 0x3F;
            return command;
        }
    }
}
