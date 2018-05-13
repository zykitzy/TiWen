using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        private ObservableCollection<LableInfo> data;

        public ObservableCollection<LableInfo> Data
        {
            get => data;
            set
            {
                data = value;

            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            data = new ObservableCollection<LableInfo>();
            Data.Add(new LableInfo() { LableID="test", LableValue="1111",ConvertValue="测试" });

            listShow.ItemsSource = Data;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (data.Count > 15)
            {
                data.RemoveAt(0);
            }
            var info = new LableInfo();
            info.LableID = timertick.ToString();
            info.LableValue = $"第{timertick}秒";
            info.ConvertValue = new Random().Next(timertick).ToString();
            Data.Add(info);
            listShow.ItemsSource = Data;
            timertick++;

        }

        private void btnMontor_Click(object sender, RoutedEventArgs e)
        {
            ismonitor = !ismonitor;
            if (ismonitor)
            {
                timer.Start();
                btnMontor.Content = "取消监听";
                btnMontor.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                timer.Stop();
                btnMontor.Content = "开始监听";
                btnMontor.Foreground = new SolidColorBrush(Colors.Black);
            }


        }

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
    }
}
