using Com.OCAMAR.Common.Library;
using ConsoleData.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        static int count;
        //分给每个终端的时间片
        //以后要修改的话，此字典应该分成多个，体温，输液等
        static Dictionary<EndPoint, Tuple<int, DateTime>> timetiff;
        static Queue mqbuffer;
        static void Main(string[] args)
        {
            SysHelper.SetRemind("输出EXIT 退出");
            var listen = new ListenClass();
            listen.startListen();
            mqbuffer = new Queue();
            string exit;
            timetiff = new Dictionary<EndPoint, Tuple<int, DateTime>>();
            while (true)
            {
                exit = Console.ReadLine();
                if (exit.ToLower() == "exit")
                {
                    break;
                }
            }

        }

        public class ListenClass
        {
            //将远程连接的客户端的IP地址和Socket存入集合中  
            System.Timers.Timer sendTimer;
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
                    SysHelper.SetNews($"监听成功,端口：{pointstr}");
                //    LogWriter.Info($"监听成功,端口：{pointstr}");
                    socketWatch.Listen(1000);

                    sendTimer = new System.Timers.Timer(6000);
                    sendTimer.Elapsed += SendTimer_Elapsed;
                    sendTimer.Start();


                    Thread th = new Thread(Listen);
                    th.IsBackground = true;
                    th.Start(socketWatch);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception ex)
                {
                    SysHelper.SetWarning(ex.ToString());
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
                        //dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                        //开启 一个新线程不停的接受客户端发送过来的消息  
                        Thread th = new Thread(Recive);
                        th.IsBackground = true;
                        th.Start(socketSend);
                    }
                    catch (SocketException ex)
                    {
                        SysHelper.SetWarning("链接发生错误！");
                        LogWriter.Error(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        SysHelper.SetWarning(ex.ToString());
                        LogWriter.Error(ex.ToString());
                    }
                }
            }


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
                        //    Console.WriteLine(socketSend.RemoteEndPoint.ToString());
                        //数据先放入队列，之后再做处理
                        mqbuffer.Enqueue(buffer);
                        count++;
                        if (buffer[16].ToString("X2") == "3F" && buffer[2].ToString("X2") == "22")
                        {
                            //判断数据是否合法
                            if (CheckHelper.CheckData(buffer))
                            {
                                if (!timetiff.ContainsKey(socketSend.RemoteEndPoint))
                                {
                                    timetiff.Add(socketSend.RemoteEndPoint,
                                        new Tuple<int, DateTime>(0, DateTime.Now));
                                }
                                else if ((DateTime.Now - timetiff[socketSend.RemoteEndPoint].Item2).Seconds <= 3)
                                {
                                    continue;
                                }
                                // 发送标签ID
                                var tagid = timetiff[socketSend.RemoteEndPoint];
                                //3秒内相同的标签只能上线一个，因为要下发ID
                                var initID = timetiff[socketSend.RemoteEndPoint].Item1 + 1;
                                if (initID > 150)
                                {
                                    SysHelper.SetWarning($"终端个数{initID}超过150，阻止上线");
                                    continue;
                                }
                                //响应昂科数据
                                var response = OcaMarHelper.RecLocalTag(buffer);
                                socketSend.Send(response);
                                SysHelper.SetNews($"响应基站数据");


                                var tag = OcaMarHelper.GetTagID(buffer, initID);
                                timetiff[socketSend.RemoteEndPoint] = new Tuple<int, DateTime>(initID, DateTime.Now);
                                socketSend.Send(tag);

                                SysHelper.SetNews($"下发终端ID：{initID}");
                            }
                            else
                            {
                                SysHelper.SetRemind($"同步昂科数据时，验证不通过！");
                            }
                        }

                    }
                    catch (SocketException ex)
                    {
                        LogWriter.Error(ex.ToString());
                        SysHelper.SetWarning("链接发生错误,关闭此链接");
                        socketSend.Shutdown(SocketShutdown.Both);
                        socketSend.Close();
                        Thread.CurrentThread.Join();
                        sendTimer = new System.Timers.Timer(10000);
                        sendTimer.Elapsed += SendTimer_Elapsed;
                        sendTimer.Start();

                    }
                    catch (Exception ex)
                    {
                        SysHelper.SetWarning(ex.ToString());
                    }
                }
                SysHelper.SetWarning("通信线程关闭！");

                // 关闭监听和重启线程
                socketSend.Shutdown(SocketShutdown.Both);
                socketSend.Close();

                sendTimer = new System.Timers.Timer(10000);
                sendTimer.Elapsed += SendTimer_Elapsed;
                sendTimer.Start();
            }


            /// <summary>
            /// 获取线程安全的同步队列数据
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void SendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                Queue dicnew = new Queue();
                lock (mqbuffer)
                {
                    dicnew = Queue.Synchronized(mqbuffer);
                }
                int currentcount = dicnew.Count;
                SendData(dicnew, currentcount);
            }

            /// <summary>
            /// 主函数，发送数据
            /// </summary>
            /// <param name="mqbuffer"></param>
            /// <param name="fillcount"></param>
            protected void SendData(Queue mqbuffer, int fillcount)
            {
                try
                {

                    int logCount = fillcount > 255 ? 255 : fillcount;

                    // 一次组织所有的体温数据
                    byte[] tiwen = new byte[logCount * 27];
                    while (logCount > 0 && mqbuffer.Count > 0)
                    {
                        byte[] data = (byte[])mqbuffer.Dequeue();
                        if (data[2].ToString("X2") == "27")
                        {
                            //判断数据是否合法
                            if (CheckHelper.CheckData(data))
                            {
                                var single = new byte[27];
                                single = CaDiHelper.GetTiwenTag(data);
                                if (single != null)
                                    Buffer.BlockCopy(single, 0, tiwen, (logCount - mqbuffer.Count - 1) * 27, 27);
                            }
                            else
                            {
                                SysHelper.SetRemind($"昂科体温数据，验证不通过！");
                            }
                        }
                        fillcount--;
                    }
                    //发送卡迪数据
                    CaDiHelper.SendCopyCadi(tiwen);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

        }
    }
}
