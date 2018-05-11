using System;
using System.Collections.Generic;
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

namespace DataChange
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnMontor_Click(object sender, RoutedEventArgs e)
        {

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
