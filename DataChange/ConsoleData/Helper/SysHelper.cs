using Com.OCAMAR.Common.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleData.Helper
{
    public static class SysHelper
    {
        /// <summary>
        /// 设置控制台颜色再输出消息
        /// </summary>
        /// <param name="color1">起始颜色</param>
        /// <param name="color2">输出后颜色</param>
        public static void SetConsole(string msg,ConsoleColor color1, ConsoleColor color2)
        {
            Console.ForegroundColor = color1;
            Console.WriteLine(msg);
            Console.ForegroundColor = color2;
        }

        /// <summary>
        /// 输出控制台颜色后，自动变回白色
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="color"></param>
        public static void SetConsole(string msg, ConsoleColor color)
        {
            SetConsole(msg, color, ConsoleColor.White);
        }

        /// <summary>
        /// 输出控制台警告
        /// </summary>
        /// <param name="msg"></param>
        public static void SetWarning(string msg)
        {
            SetConsole(msg,ConsoleColor.Red);
            LogWriter.Error(msg);
        }

        /// <summary>
        /// 输出控制台提醒
        /// </summary>
        /// <param name="msg"></param>
        public  static void SetRemind(string msg)
        {
            SetConsole(msg, ConsoleColor.Yellow);
            LogWriter.Info(msg);
        }

        /// <summary>
        /// 输出控制台消息
        /// </summary>
        /// <param name="msg"></param>
        public static void SetNews(string msg)
        {
            SetConsole(msg, ConsoleColor.Green);
            LogWriter.Info(msg);
        }
    }
}
