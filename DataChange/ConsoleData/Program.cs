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
                    int i = 8; //包头位移
                    byte[] response = new byte[35];
                    //前八位是基站头
                    //获取卡迪基站模拟,必须空格分割
                    var tagServer = ConfigurationManager.AppSettings["TagServer"].Split(' ');
                    if (tagServer.Count() != 6)
                    {
                        throw new Exception("TagServer 无法模拟，必须是6个字节以空格分隔");
                    }
                    for (int f = 0; f < 6; f++)
                    {
                        response[f] = (byte)Convert.ToInt32($"0x{tagServer[f]}", 16);
                    }
                    //后两位是本次报文数据量，默认每次发送一条
                    response[6] = 0x01;
                    response[7] = 0x00;
                    //参数参考 TSS 800，FD固定，Normal 状态为 00
                    response[0 + i] = 0xFD;
                    response[1 + i] = 0x00;
                    // 第三位到第八位是tag，通过config 匹配 昂科标签
                    var tags = ConfigurationManager.AppSettings["TagClient"].Split(',');
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
                                response[f + i] = (byte)Convert.ToInt32($"0x{tag.Split('|')[1].Split(' ')[f - 2]}", 16);
                            }
                        }
                        break;
                    }
                    //以下 五位未知，可能影响时间
                    response[8 + i] = 0x00;
                    response[9 + i] = 0x23;
                    response[10 + i] = 0x10;
                    response[11 + i] = 0x00;
                    response[12 + i] = 0xFB;
                    //以下4位温度，小数点第三位默认0
                    response[13 + i] = buffer[12];
                    int t10 = buffer[13];
                    response[14 + i] = (byte)(t10 / 10);
                    response[15 + i] = (byte)(t10 % 10);
                    response[16 + i] = 0x00;
                    //Kadi 预留位
                    response[17 + i] = 0x00;
                    response[18 + i] = 0x00;
                    response[19 + i] = 0x05;
                    response[20 + i] = 0x00;
                    // 时间参数
                    response[21 + i] = (byte)DateTime.Now.Day;
                    response[22 + i] = (byte)DateTime.Now.Month;
                    //年份 20XX
                    response[23 + i] = (byte)(DateTime.Now.Year - 2000);
                    response[24 + i] = (byte)DateTime.Now.Hour;
                    response[25 + i] = (byte)DateTime.Now.Minute;
                    response[26 + i] = (byte)DateTime.Now.Second;
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
                            + response[13 + i] + "." //整数
                            + response[14 + i] //小数第一位
                            + response[15 + i]); //小数第二位
                        Console.WriteLine($"时间 : {response[23 + i]} 年" +
                            $"{response[22 + i]} 月" +
                            $"{response[21 + i]} 日" +
                            $"{response[24 + i]} :" +
                            $"{response[25 + i]} :" +
                            $"{response[26 + i]} :");
                        socket.Send(response, 35, SocketFlags.None);

                        Console.WriteLine("disconnect from server");
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                    //异常处理
                    catch (SocketException e)
                    {
                        Console.WriteLine("Fail to connect server");
                        Console.WriteLine(e.ToString());
                        return;
                    }
                }
            }
            string FormatByte(byte btr)
            {
                return btr.ToString("X2");
            }

        }
    }
}
