using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    public static class ColorfulConsole
    {
        /// <summary>
        /// 设置终端输出的颜色
        /// </summary>
        /// <param name="fcolor">前景色</param>
        /// <param name="bcolor">背景色</param>
        static void SetColor(ConsoleColor? fcolor = null, ConsoleColor? bcolor = null)
        {
            try
            {
                if (fcolor is ConsoleColor fc)
                    Console.ForegroundColor = fc;
                if(bcolor is ConsoleColor bc)
                    Console.BackgroundColor = bc;
            }
            catch { }
        }

        /// <summary>
        /// 输出带颜色的字符串
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="fcolor">前景色</param>
        /// <param name="bcolor">背景色</param>
        public static void WriteLine(string str, ConsoleColor? fcolor = null, ConsoleColor? bcolor = null)
        {
            SetColor(fcolor, bcolor);
            Console.WriteLine(str);
        }

        /// <summary>
        /// 输出带颜色的字符串
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="fcolor">前景色</param>
        /// <param name="bcolor">背景色</param>
        public static void Write(string str, ConsoleColor? fcolor = null, ConsoleColor? bcolor = null)
        {
            SetColor(fcolor, bcolor);
            Console.Write(str);
        }

        public static void Log(string str) => Write(str, ConsoleColor.White, ConsoleColor.Black);
        public static void LogLine(string str) => WriteLine(str, ConsoleColor.White, ConsoleColor.Black);
        public static void Info(string str) => Write(str, ConsoleColor.Yellow, ConsoleColor.Black);
        public static void InfoLine(string str) => WriteLine(str, ConsoleColor.Yellow, ConsoleColor.Black);
        public static void Warn(string str) => Write(str, ConsoleColor.Red, ConsoleColor.Black);
        public static void WarnLine(string str) => WriteLine(str, ConsoleColor.Red, ConsoleColor.Black);
        public static void Success(string str) => Write(str, ConsoleColor.Green, ConsoleColor.Black);
        public static void SuccessLine(string str) => WriteLine(str, ConsoleColor.Green, ConsoleColor.Black);


    }
}
