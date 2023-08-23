using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class ReadFlash
    {
        private static ReadFlashParameter readFlashParameter;

        static ReadFlash()
        {
            readFlashParameter = new ReadFlashParameter();
        }

        public static void SetReadFlashParameter(ReadFlashParameter parameter)
        {
            readFlashParameter = parameter;
        }

        /// <summary>
        /// 读取固件
        /// </summary>
        /// <returns></returns>
        public static bool Read()
        {

            //检查传入的文件名
            var fi = new FileInfo(readFlashParameter.Filename);
            if(fi.Extension != ".bin")
            {
                ColorfulConsole.WarnLine($"Please enter the correct [.bin] filename.");
                Environment.Exit(0);
            }
            if(File.Exists(readFlashParameter.Filename)) 
            { 
                if(readFlashParameter.Overwrite)
                {
                    File.Delete(readFlashParameter.Filename);
                }
                else
                {
                    ColorfulConsole.WarnLine($"File [{readFlashParameter.Filename}] already exists.");
                    Environment.Exit(0);
                }
            }

            var file = File.OpenWrite(readFlashParameter.Filename);//创建文件

            BasicOperation.ResetBootloader();

            //处理下数据长度

            //刷代码进去
            ColorfulConsole.LogLine("start read data ...");
            var nowTime = DateTime.Now;

            int baseAddress;
            if (readFlashParameter.Address.StartsWith("0x") == true || readFlashParameter.Address.StartsWith("0X") == true) //0x开头，说明是16进制
            {
                baseAddress = Convert.ToInt32(readFlashParameter.Address, 16);
            }
            else //10进制
            {
                baseAddress = Convert.ToInt32(readFlashParameter.Address, 10);
            }
            var now = baseAddress;
            var latestAddr = now + readFlashParameter.Length;//固件长度
            while (now < latestAddr)
            {
                var command = new byte[] { (byte)BasicOperation.Command.ReadMemory, (byte)~BasicOperation.Command.ReadMemory };

                var cmdDone = false;
                for(int i=0;i<3 ;i++)
                {
                    if (BasicOperation.Write(command, 10) == false)
                    {
                        //发一个字节抵消一下isp里的缓冲区
                        BasicOperation.Write(new byte[] { 0x7f }, 10);
                    }
                    else
                    {
                        cmdDone = true;
                        break;
                    }
                }
                if(!cmdDone)
                {
                    ColorfulConsole.WarnLine($"prepare writing to address 0x{now:X} failed.");
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
                    ColorfulConsole.WarnLine($"set read address 0x{now:X} failed.");
                    return false;
                }

                byte len = 0xff;
                if (now + len > latestAddr)
                {
                    len = (byte)(latestAddr - now);
                }
                var lengthData = new byte[4] { len, 0,0, len };
                var r = BasicOperation.WriteAndRead(lengthData, 500);
                if (r == null || r[0] != 0x79 || r.Length != lengthData[0] + 2)
                {
                    ColorfulConsole.WarnLine($"read address 0x{now:X} failed.");
                    return false;
                }
                file.Write(r,1, lengthData[0] + 1);

                now += 0x100;

                if (readFlashParameter.NoProgress == false) //如果没有禁用进度条打印
                {
                    // 清除终端的这一行的打印
                    if (BasicOperation.baseParameter.Trace == false)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        int currentLineCursor = Console.CursorTop;
                        Console.SetCursorPosition(0, Console.CursorTop);
                        ColorfulConsole.Log(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, currentLineCursor);
                    }
                    ColorfulConsole.InfoLine($"Reading at {now}... {(double)(now - baseAddress) / (latestAddr - baseAddress) * 100:f2}%");
                }
            }
            file.Close();
            ColorfulConsole.SuccessLine($"Read {readFlashParameter.Length} bytes from {readFlashParameter.Address} in {DateTime.Now.Subtract(nowTime).TotalMilliseconds} ms");
            ColorfulConsole.LogLine("");
            //正常重启进入运行模式
            BasicOperation.ResetAPP();

            return true;
        }
    }
}
