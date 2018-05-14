using Com.OCAMAR.Common.Library;
using DataChange.SocketConnection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DataChange
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool ismonitor = false;
        private DispatcherTimer timer;
        private int timertick = 0;
        public ObservableCollection<LableInfo> Data { get; set; }
        //长连接的对象
        Socket socketSend;
        //当点击开始监听的时候 在服务器端创建一个负责监IP地址跟端口号的Socket  
        Socket socketWatch;
        //将远程连接的客户端的IP地址和Socket存入集合中  
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = Data;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            Data = new ObservableCollection<LableInfo>();
            //  Data.Add(new LableInfo() { LableID = "test", LableValue = "1111", ConvertValue = "测试" });

            listShow.ItemsSource = Data;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Data.Count > 10)
            {
                Data.RemoveAt(0);
            }


        }

        private void btnMontor_Click(object sender, RoutedEventArgs e)
        {
            if (!ismonitor)
            {
                if (!StartListen())
                    return;
                timer.Start();
                btnMontor.Content = "监听中...";
                btnMontor.Foreground = new SolidColorBrush(Colors.Red);
                btnMontor.IsEnabled = false;
            }
            else
            {
                timer.Stop();
                btnMontor.Content = "开始监听";
                btnMontor.Foreground = new SolidColorBrush(Colors.Black);
                DisposeSocket();

            }

            ismonitor = !ismonitor;
        }

        /// <summary>
        ///开始监听
        /// </summary>
        private bool StartListen()
        {
            if (string.IsNullOrEmpty(txtRequest.Text))
            {
                MessageBox.Show("请输入端口号");
                txtRequest.Focus();
                return false;
            }

            if (socketWatch != null)
            {
                if (socketWatch.Connected)
                    socketWatch.Shutdown(SocketShutdown.Both);
                socketWatch.Close();
                socketWatch = null;
            }
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;//IPAddress.Parse(txtServer.Text);  
                                         //创建端口号对象                           
            IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(txtRequest.Text));

            //监听  
            socketWatch.Bind(point);
            socketWatch.Listen(1000);
            Thread th = new Thread(Listen);
            th.IsBackground = true;
            th.Start(socketWatch);
            return true;
        }

        /// <summary>  
        /// 等待客户端的连接 并且创建与之通信用的Socket  
        /// </summary>  
        private void Listen(object o)
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
                    MessageBox.Show(ex.ToString());
                    LogWriter.Error(ex.ToString());
                }
            }
        }
        /// <summary>  
        /// 服务器端不停的接受客户端发送过来的消息  
        /// </summary>  
        /// <param name="o"></param>  
        private void Recive(object o)
        {
            Socket socketSend = o as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器应该接受客户端发来的消息  
                    byte[] buffer = new byte[17];
                    //实际接受到的有效字节数  
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    //体温标签
                    foreach (byte chars in buffer)
                    {
                        MessageBox.Show(chars.ToString());
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    LogWriter.Error(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 关闭SOCKET
        /// </summary>
        private void DisposeSocket()
        {
            if (socketSend != null)
            {
                if (socketSend.Connected)
                {
                    socketSend.Shutdown(SocketShutdown.Both);
                }
                socketSend.Close();
                socketSend.Dispose();
            }
            if (socketWatch != null)
            {
                if (socketWatch.Connected)
                {
                    socketWatch.Shutdown(SocketShutdown.Both);
                }
                socketWatch.Close();
                socketWatch.Dispose();
            }

            var threads = Process.GetCurrentProcess().Threads;
            foreach(ProcessThread thread in threads)
            {
                if(thread.Id != Thread.CurrentThread.ManagedThreadId)
                {
                    thread.Dispose();
                }
            }
        }

        #region 输入限制
        /// <summary>
        /// 限制输入数字和.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtRequest_KeyDown(object sender, KeyEventArgs e)
        {
            if (!IsNum(e.Key))
                e.Handled = true;
        }

        private bool IsNum(Key input)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                return false;
            }
            if (input >= Key.D0 && input <= Key.D9)
                return true;
            else if (input >= Key.NumPad0 && input <= Key.NumPad9)
                return true;
            else if (input == Key.Back)
                return true;
            else if (input == Key.Decimal)
                return true;
            else if (input == Key.OemPeriod)
                return true;
            return false;
        }
        #endregion
    }
}
