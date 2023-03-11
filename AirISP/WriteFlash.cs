using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class WriteFlashClass
    {
        private static WriteFlashParameter writeFlashParameter;

        static WriteFlashClass()
        {
            writeFlashParameter = new WriteFlashParameter();
        }

        public static void SetWriteFlashParameter(WriteFlashParameter parameter)
        {
            writeFlashParameter = parameter;
        }

        /// <summary>
        /// 烧录固件的函数
        /// </summary>
        /// <returns></returns>
        public static bool WriteFlash()
        {

            //读取固件数据
            var data = File.ReadAllBytes(writeFlashParameter.Filename!);
            BasicOperation.ResetBootloader();

            //擦除数据
            if (writeFlashParameter.EraseAll == true)
            {
                EraseFlashClass.EraseFlash();
            }
            //处理下固件数据
            //数据不够0x80字节对齐？
            if (data.Length % 0x80 != 0)
            {
                var d = new List<byte>();
                d.AddRange(data);
                while (d.Count % 0x80 != 0)
                    d.Add(0xff);
                data = d.ToArray();
            }
            //刷代码进去
            Console.WriteLine("start write data ...");
            var nowTime = DateTime.Now;

            int baseAddress;
            if (writeFlashParameter.Address.StartsWith("0x") == true || writeFlashParameter.Address.StartsWith("0X") == true) //0x开头，说明是16进制
            {
                baseAddress = Convert.ToInt32(writeFlashParameter.Address, 16);
            }
            else //10进制
            {
                baseAddress = Convert.ToInt32(writeFlashParameter.Address, 10);
            }
            var now = baseAddress;
            var latestAddr = now + data.Length;//固件长度
            while (now < latestAddr)
            {
                var command = new byte[] { (byte)BasicOperation.Command.WriteMemory, (byte)~BasicOperation.Command.WriteMemory };
                if (BasicOperation.Write(command, 500) == false)
                {
                    Console.WriteLine($"prepare writing to address 0x{now:X} failed.");
                    return false;
                }
                var addrBytes = new byte[5] {
                    (byte)(now/256/256/256),
                    (byte)(now/256/256%256),
                    (byte)(now/256%256), //高八位
                    (byte)(now %256), //第八位
                    0
                };
                for (int i = 0; i < 4; i++)//校验和
                {
                    addrBytes[4] = (byte)(addrBytes[4] ^ addrBytes[i]);
                }
                if (BasicOperation.Write(addrBytes) == false)
                {
                    Console.WriteLine($"set write address 0x{now:X} failed.");
                    return false;
                }

                var flashData = new byte[1 + 128 + 1];
                flashData[0] = 0x7f;
                byte xor = 0x7f;
                for (int i = 0; i < 0x80; i++)
                {
                    flashData[i + 1] = data[now - baseAddress + i];
                    xor = (byte)(xor ^ data[now - baseAddress + i]);
                }
                flashData[flashData.Length - 1] = xor;
                if (BasicOperation.Write(flashData, 500) == false)
                {
                    Console.WriteLine($"writing to address 0x{now:X} failed.");
                    return false;
                }

                now += 0x80;

                if (writeFlashParameter.NoProgress == false) //如果没有禁用进度条打印
                {
                    // 清除终端的这一行的打印
                    if(BasicOperation.baseParameter.Trace == false)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        int currentLineCursor = Console.CursorTop;
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, currentLineCursor);
                    }
                    Console.WriteLine($"Writing at {now}... {(double)(now - baseAddress) / (latestAddr - baseAddress) * 100:f2}%");
                }
            }
            Console.WriteLine($"Write {data.Length} bytes at {writeFlashParameter.Address} in {DateTime.Now.Subtract(nowTime).TotalMilliseconds} ms");
            Console.WriteLine("");
            //正常重启进入运行模式
            BasicOperation.ResetAPP();

            return true;
        }
    }
}
