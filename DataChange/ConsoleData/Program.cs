using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleData
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            var listen = new ListenClass();
            Console.WriteLine("输入EXIT退出");
            listen.startListen();
            string exit;
            while (true)
            {
                exit = Console.ReadLine();
                if (exit == "EXIT")
                {
                    break;
                }
            }
        }

        public class ListenClass
        {
            /// <summary>
            /// 开始监听
            /// </summary>
            public void startListen()
            {
                try
                {
                    //当点击开始监听的时候 在服务器端创建一个负责监IP地址跟端口号的Socket  
                    Socket socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPAddress ip = IPAddress.Any;//IPAddress.Parse(txtServer.Text);  
                                                 //创建端口号对象 
                    string pointstr = ConfigurationManager.AppSettings["ListenPort"];
                    IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(pointstr));
                    //监听  
                    socketWatch.Bind(point);
                    Console.WriteLine($"监听成功,端口：{pointstr}");
                    socketWatch.Listen(1000);

                    Thread th = new Thread(Listen);
                    th.IsBackground = true;
                    th.Start(socketWatch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            /// <summary>  
            /// 等待客户端的连接 并且创建与之通信用的Socket  
            /// </summary>  
            ///   
            Socket socketSend;
            void Listen(object o)
            {
                Socket socketWatch = o as Socket;
                //等待客户端的连接 并且创建一个负责通信的Socket  
                while (true)
                {
                    try
                    {
                        //负责跟客户端通信的Socket  
                        socketSend = socketWatch.Accept();
                        //将远程连接的客户端的IP地址和Socket存入集合中  
                        dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                        //开启 一个新线程不停的接受客户端发送过来的消息  
                        Thread th = new Thread(Recive);
                        th.IsBackground = true;
                        th.Start(socketSend);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            //将远程连接的客户端的IP地址和Socket存入集合中  
            Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();
            /// <summary>  
            /// 服务器端不停的接受客户端发送过来的消息  
            /// </summary>  
            /// <param name="o"></param>  
            void Recive(object o)
            {
                Socket socketSend = o as Socket;
                while (true)
                {
                    try
                    {
                        //客户端连接成功后，服务器应该接受客户端发来的消息  
                        byte[] buffer = new byte[25];
                        //实际接受到的有效字节数  
                        int r = socketSend.Receive(buffer);
                        if (r == 0)
                        {
                            break;
                        }
                        //响应昂科数据
                        RecLocalTag(buffer);
                        Console.ForegroundColor = ConsoleColor.White;
                        //发送卡迪数据
                        SendUseKadi(buffer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            /// <summary>
            /// 得到昂科基站心跳，并且响应
            /// </summary>
            /// <param name="buffer">响应流</param>
            protected void RecLocalTag(byte[] buffer)
            {
                if (buffer[16].ToString("X2") == "3F" && (buffer[2].ToString("X2") == "22" || buffer[2].ToString("X2") == "27"))
                {
                    StringBuilder sb = new StringBuilder();
                    Console.ForegroundColor = ConsoleColor.Green;
                    sb.Append(DateTime.Now.ToString() + " ：");
                    for (int i = 0; i <= 16; i++)
                    {
                        sb.Append(buffer[i].ToString("X2"));
                        sb.Append(" ");
                    }
                    Console.WriteLine(sb);
                    if (buffer[2].ToString("X2") == "22")
                    {
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

                        socketSend.Send(response);
                        Console.WriteLine("响应基站同步请求");
                    }
                }
            }

            /// <summary>
            /// 模仿卡迪发送
            /// </summary>
            /// <param name="buffer"></param>
            protected void SendUseKadi(byte[] buffer)
            {
                //模拟Kadi 发送
                if (buffer[2].ToString("X2") == "27")
                {
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
                    var tiwen = GetTiwenTag(buffer);
                    // 配置基站心跳
                    var xintiao = ServerXinTiao(node);
                    //基站IP
                    var serverip = ServerIP(node);

                    StringBuilder sb = new StringBuilder();
                    foreach (var no in node)
                    {
                        sb.Append(no.ToString("X2"));
                        sb.Append(" ");
                    }
                    Console.WriteLine("基站 : " + sb);

                    byte[] response = new byte[node.Length + 2 + tiwen.Length 
                                                    + xintiao.Length + serverip.Length];
                    Buffer.BlockCopy(node, 0, response, 0, 6);
                    //后两位是本次报文数据量
                    //1 体温 2 心跳 3 基站IP
                    response[6] = 0x03;
                    response[7] = 0x00;

                    Buffer.BlockCopy(tiwen, 0, response, 8, 27);
                    sb.Clear();
                    foreach (var no in tiwen)
                    {
                        sb.Append(no.ToString("X2"));
                        sb.Append(" ");
                    }
                    Console.WriteLine("体温 : " + sb);

                    Buffer.BlockCopy(xintiao, 0, response, 35, 27);
                    sb.Clear();

                    foreach (var no in xintiao)
                    {
                        sb.Append(no.ToString("X2"));
                        sb.Append(" ");
                    }
                    Console.WriteLine("心跳 : " + sb);

                    Buffer.BlockCopy(serverip, 0, response, 62, 27);
                    sb.Clear();
                    foreach (var no in serverip)
                    {
                        sb.Append(no.ToString("X2"));
                        sb.Append(" ");
                    }
                    Console.WriteLine("基站IP : " + sb);

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
                        Console.WriteLine("体温 : "
                            + response[21] + "." //整数
                            + response[22] //小数第一位
                            + response[23]); //小数第二位
                        Console.WriteLine($"时间 : {response[31]} 年" +
                            $"{response[30]} 月" +
                            $"{response[29]} 日" +
                            $"{response[32]} :" +
                            $"{response[33]} :" +
                            $"{response[34]}");

                        socket.Send(response, response.Length, SocketFlags.None);

                        Console.WriteLine($"Send {response.Length} Byte To {ipaddress} Success!");
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        Console.WriteLine($"Disconnect Server");
                    }
                    //异常处理
                    catch (SocketException e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Fail to connect server");
                        Console.WriteLine(e.ToString());
                        return;
                    }
                }
            }

            /// <summary>
            /// 配置体温标签
            /// </summary>
            /// <param name="buffer"></param>
            /// <returns></returns>
            private byte[] GetTiwenTag(byte[] buffer)
            {
                var response = new byte[27];
                //参数参考 TSS 800，FD固定，Normal 状态为 00
                response[0] = 0xFD;
                response[1] = 0x00;


                // 第三位到第八位是tag，通过config 匹配 昂科标签
                var tags = ConfigurationManager.AppSettings["TagClient"].Replace("\r\n", "").Split(',');
                foreach (var tag in tags)
                {
                    //匹配标签，昂科标签转Kadi, | 前为昂科标签，后卫卡迪标签
                    if (buffer[7] == (byte)Convert.ToInt32((tag.Split('|')[0].Split(' ')[0]), 16)
                        && buffer[8] == (byte)Convert.ToInt32((tag.Split('|')[0].Split(' ')[1]), 16)
                        && buffer[9] == (byte)Convert.ToInt32((tag.Split('|')[0].Split(' ')[2]), 16)
                        && buffer[10] == (byte)Convert.ToInt32((tag.Split('|')[0].Split(' ')[3]), 16)
                        )
                    {
                        for (int f = 2; f <= 7; f++)
                        {
                            response[f] = (byte)Convert.ToInt32($"0x{tag.Split('|')[1].Split(' ')[f - 2]}", 16);
                        }
                        break;
                    }
                    //    continue;
                }
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

                return response;
            }

            /// <summary>
            /// 心跳报文
            /// </summary>
            /// <param name="server">基站6字节编码</param>
            /// <returns></returns>
            private byte[] ServerXinTiao(byte[] server)
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

            //发送IP数据
            private byte[] ServerIP(byte[] node)
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



        }
    }
}
