using Com.OCAMAR.Common.Library;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleData.Helper
{
    public static class CaDiHelper
    {

        // <summary>
        /// 配置体温标签
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static byte[] GetTiwenTag(byte[] buffer)
        {
            var response = new byte[27];

            if (buffer[2].ToString("X2") == "27")
            {

                // 第三位到第八位是tag，通过config 匹配 昂科标签
                var tags = ConfigurationManager.AppSettings["TagClient"].Replace("\r\n", "").Split(',');
                int tagcount = 0;
                foreach (var tag in tags)
                {
                    var biaoqian1 = Convert.ToByte((tag.Split('|')[0].Split(' ')[0]), 16);
                    var biaoqian2 = Convert.ToByte((tag.Split('|')[0].Split(' ')[1]), 16);
                    var biaoqian3 = Convert.ToByte((tag.Split('|')[0].Split(' ')[2]), 16);
                    var biaoqian4 = Convert.ToByte((tag.Split('|')[0].Split(' ')[3]), 16);
                    //匹配标签，昂科标签转Kadi, | 前为昂科标签，后卫卡迪标签
                    if (buffer[7] == biaoqian1
                        && buffer[8] == biaoqian2
                        && buffer[9] == biaoqian3
                        && buffer[10] == biaoqian4
                        )
                    {
                        tagcount++;
                        for (int f = 2; f <= 7; f++)
                        {
                            response[f] = Convert.ToByte($"{tag.Split('|')[1].Split(' ')[f - 2]}", 16);
                        }
                        break;
                    }
                    //    continue;
                }
                if (tagcount == 0)
                {
                    SysHelper.SetRemind($"标签 {buffer[7].ToString("X2")} " +
                        $"{buffer[8].ToString("X2")} " +
                        $"{buffer[9].ToString("X2")} " +
                        $"{buffer[0].ToString("X2")} 无法匹配！");
                    return null;
                }
                //参数参考 TSS 800，FD固定，Normal 状态为 00
                response[0] = 0xFD;
                response[1] = 0x00;


                //以下 五位未知，可能影响
                response[8] = 0x00;
                response[9] = 0x23;
                response[10] = 0x10;
                response[11] = 0x00;
                response[12] = 0xFB;
                //以下4位温度，小数点第三位默认0
                response[13] = buffer[12];
                int t10 = buffer[13];
                response[14] = (byte)(t10 / 10);
                response[15] = (byte)(t10 % 10);
                response[16] = 0x00;
                //Kadi 预留位
                response[17] = 0x00;
                response[18] = 0x00;
                response[19] = 0x05;
                response[20] = 0x00;
                // 时间参数
                response[21] = (byte)DateTime.Now.Day;
                response[22] = (byte)DateTime.Now.Month;
                //年份 20XX
                response[23] = (byte)(DateTime.Now.Year - 2000);
                response[24] = (byte)DateTime.Now.Hour;
                response[25] = (byte)DateTime.Now.Minute;
                response[26] = (byte)DateTime.Now.Second;


            }
            return response;
        }

        /// <summary>
        /// 心跳报文
        /// </summary>
        /// <param name="server">基站6字节编码</param>
        /// <returns></returns>
        public static byte[] ServerXinTiao(byte[] server)
        {
            var node = new byte[27];
            //心跳前两位固定
            node[0] = 0x23;
            node[1] = 0x00;
            //六位基站
            Buffer.BlockCopy(server, 0, node, 2, 6);
            // 8-19 固定编码
            node[8] = 0x00;
            node[9] = 0x00;
            node[10] = 0x00;
            node[11] = 0x00;
            node[12] = 0x0A;
            node[13] = 0x0A;
            node[14] = 0x05;
            node[15] = 0x1B;
            node[16] = 0x00;
            node[17] = 0x00;
            node[18] = 0x00;
            node[19] = 0x00;
            //20 位 net 固定 00 Normal
            node[20] = 0x00;
            //6位时间
            // 时间参数
            node[21] = (byte)DateTime.Now.Day;
            node[22] = (byte)DateTime.Now.Month;
            //年份 20XX
            node[23] = (byte)(DateTime.Now.Year - 2000);
            node[24] = (byte)DateTime.Now.Hour;
            node[25] = (byte)DateTime.Now.Minute;
            node[26] = (byte)DateTime.Now.Second;
            return node;
        }

        /// <summary>
        /// 发送IP数据
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static byte[] ServerIP(byte[] node)
        {
            var tagServer = ConfigurationManager.AppSettings["TagServer"].Split(',')[1].Split('.');
            var serverip = new byte[27];
            //IP 第一位固定
            serverip[0] = 0xFE;
            for (int i = 0; i < 4; i++)
            {
                serverip[i + 1] = (byte)Convert.ToInt32(tagServer[i]);
            }
            Buffer.BlockCopy(node, 0, serverip, 5, 6);
            //11到20位固定为0
            for (int i = 11; i < 21; i++)
            {
                serverip[i] = 0x00;
            }
            //6位时间
            // 时间参数
            serverip[21] = (byte)DateTime.Now.Day;
            serverip[22] = (byte)DateTime.Now.Month;
            //年份 20XX
            serverip[23] = (byte)(DateTime.Now.Year - 2000);
            serverip[24] = (byte)DateTime.Now.Hour;
            serverip[25] = (byte)DateTime.Now.Minute;
            serverip[26] = (byte)DateTime.Now.Second;

            return serverip;
        }

        /// <summary>
        /// 模仿卡迪发送
        /// </summary>
        /// <param name="buffer">体温数据param>
        public static void SendCopyCadi(byte[] buffer)
        {
            //模拟Kadi 发送

            byte[] node = new byte[6];

            //前八位是基站头
            //获取卡迪基站模拟,必须空格分割
            var tagServer = ConfigurationManager.AppSettings["TagServer"].Split(',')[0].Split(' ');
            if (tagServer.Count() != 6)
            {
                throw new Exception("TagServer 无法模拟，必须是6个字节以空格分隔");
            }
            for (int f = 0; f < 6; f++)
            {
                node[f] = (byte)Convert.ToInt32($"0x{tagServer[f]}", 16);
            }


            //配置kadi 体温
            var tiwen = CheckHelper.CheckZero(buffer) ? new byte[0] : buffer;
            // 配置基站心跳
            var xintiao = ServerXinTiao(node);
            //基站IP
            var serverip = ServerIP(node);




            byte[] response = new byte[node.Length + 2 + tiwen.Length
                                            + xintiao.Length + serverip.Length];
            Buffer.BlockCopy(node, 0, response, 0, 6);
            //后两位是本次报文数据量
            //1 体温 2 心跳 3 基站IP
            //一次传输数据超过 256*256 报错
            //最少有2条信息
            byte[] tiwenC = CheckHelper.ConvertByte((buffer.Count() / 27) + 2);
            if (tiwenC[2] != 0x00 || tiwenC[3] != 0x00)
            {
                throw new Exception("数据传输量过大，系统无法处理");
            }
            response[6] = tiwenC[0];
            if (tiwenC.Count() > 1)
            {
                response[7] = tiwenC[1];
            }
            else
            {
                response[7] = 0x00;
            }
            //预防体温数据不存在
            if (tiwen.Count() > 0)
            {
                Buffer.BlockCopy(tiwen, 0, response, 8, tiwen.Count());
            }


            StringBuilder sb = new StringBuilder();
            sb.Clear();
            int tagcount = 0;
            foreach (var no in tiwen)
            {
                sb.Append(no.ToString("X2"));
                sb.Append(" ");
                tagcount++;
                if (tagcount == 27)
                {
                    sb.AppendLine();
                    tagcount = 0;
                }
            }

            LogWriter.Info("体温 : " + sb);
            Buffer.BlockCopy(xintiao, 0, response, 8 + tiwen.Count(), 27);
            Buffer.BlockCopy(serverip, 0, response, 35 + tiwen.Count(), 27);
            //配置发送体温APP
            var ipaddress = ConfigurationManager.AppSettings["NurseServer"];
            IPAddress ip = IPAddress.Parse(ipaddress.Split(',')[0]);
            IPEndPoint ipEnd = new IPEndPoint(ip,
                Convert.ToInt32(ipaddress.Split(',')[1]));
            //定义套接字类型
            Socket socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            //尝试连接
            try
            {
                socket.Connect(ipEnd);
                socket.Send(response, response.Length, SocketFlags.None);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

                SysHelper.SetNews($"本次发送：{tiwen.Count() / 27}条体温数据");
                SysHelper.SetNews($"Send {response.Length} Byte To {ipaddress} Success!");
                SysHelper.SetNews($"Disconnect Server");
                sb.AppendLine();
            }
            //异常处理
            catch (SocketException e)
            {
                SysHelper.SetWarning("Fail to connect server");
                SysHelper.SetWarning(e.ToString());
             //   LogWriter.Error(e.ToString());
                return;
            }

        }

    }
}
