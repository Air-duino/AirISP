using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static AirISP.BasicOperation;

namespace AirISP
{
    static class BasicOperation
    {
        public enum ReturnVal : byte
        {
            ACK = 0x79,
            NACK = 0x1F
        }

        public enum Command : byte
        {
            Get = 0x00,
            GetVersion = 0x01,
            GetID = 0x02,
            GetDeviceID = 0x03,
            ReadMemory = 0x11,
            Go = 0x21,
            WriteMemory = 0x31,
            ExtendedErase = 0x44,
            WriteProtect = 0x63,
            WriteUnrotect = 0x73,
            ReadProtect = 0x82,
            ReadUnrotect = 0x92,
        }

        public static BaseParameter baseParameter;
        private static SerialPort serial;
        private static bool serialStatus = false;//串口状态，false为关闭，true为开启

        static BasicOperation()
        {
            baseParameter = new BaseParameter();
            serial = new SerialPort();
        }

        public static void SetBaseParameter(BaseParameter parameter)
        {
            baseParameter = parameter;
        }

        public static void SetSerialPort(SerialPort port)
        {
            serial = port;
        }

        /// <summary>
        /// 使用BasicOperation类里面的方法之前需要先begin
        /// </summary>
        /// <returns></returns>
        public static bool Begin()
        {
            Console.WriteLine($"AirISP v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine($"{(Tool.IsZh() ? "串口" : "Serial port")} {baseParameter.Port}");
            if (serialStatus == false)
            {
                serialStatus = true;
                serial = new SerialPort(baseParameter.Port!, baseParameter.Baud!, Parity.Even, 8, StopBits.One);
                //打开串口
                try
                {
                    serial.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{(Tool.IsZh() ? "打开串口失败" : "Failed to open serial port")}: {ex.Message}");
                    Environment.Exit(0);
                    return false;
                }
                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool End()
        {
            if (serialStatus == true)
            {
                serialStatus = false;
                serial.Close();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 重启进入boot模式
        /// </summary>
        /// <returns></returns>
        public static bool ResetBootloader()
        {
            Console.Write(Tool.IsZh() ? "连接中..." : "Connect...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            ManualResetEvent resetEvent = new ManualResetEvent(true);
            var LogTask = new Task(async () =>
            {
                int count = 0;
                bool WriteFlag = true;
                await Task.Delay(1000);
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    resetEvent.WaitOne();
                    if (count >= 3)
                    {
                        WriteFlag = !WriteFlag;
                        count = 0;
                        await Task.Delay(1000);
                    }
                    if (WriteFlag == false)
                    {
                        Console.Write(".");
                    }
                    else
                    {
                        Console.Write("_");
                    }
                    count++;
                    await Task.Delay(200);
                }
            }, token);
            LogTask.Start();

            for (int i = 0; i < baseParameter.ConnectAttempts; i++)
            {
                serial.DiscardInBuffer();
                serial.DiscardOutBuffer();
                switch (baseParameter.Before)
                {
                    //DTR连接BOOT0，RTS连接RST
                    case "direct_connect":
                        serial.RtsEnable = true;
                        serial.DtrEnable = false;
                        Thread.Sleep(200);
                        serial.RtsEnable = false;
                        break;


                    // 采用异或电路
                    case "default_reset":
                        //windows下面时序不准确，不容易进boot模式
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                            RuntimeInformation.OSArchitecture == Architecture.X64)
                        {
                            serial.Close();
                            var p = new ProcessStartInfo("rst2boot.exe",serial.PortName) { RedirectStandardOutput = true };
                            var rst2boot = Process.Start(p);
                            using (var sr = rst2boot!.StandardOutput)
                            {
                                while (!sr.EndOfStream)
                                {
                                    Console.WriteLine(sr.ReadLine());
                                }
                                if (!rst2boot.HasExited)
                                {
                                    rst2boot.Kill();
                                }
                            }
                            serial.RtsEnable = false;
                            serial.DtrEnable = false;
                            serial.Open();
                        }
                        else
                        {
                            //传统方法
                            serial.DtrEnable = true;
                            serial.RtsEnable = false;
                            Thread.Sleep(20);

                            serial.RtsEnable = true;
                            serial.DtrEnable = false;
                            Thread.Sleep(10);

                            serial.RtsEnable = false;
                            serial.DtrEnable = true;

                            Thread.Sleep(5);

                            serial.DtrEnable = false;
                        }

                        break;

                    default: return false;
                }

                var data = new byte[] { 0x7F };

                if (Write(data) == false)
                {
                    if (BasicOperation.baseParameter.Trace == true)
                    {
                        Console.WriteLine(Tool.IsZh() ? "连接失败，重试" : "connect fail, retry.");
                    }
                    continue;
                }
                resetEvent.Reset();
                Console.WriteLine("");
                for (int j = 0; j < 3; j++)
                {
                    if (GetClass.GetID() == false)
                    {
                        if (BasicOperation.baseParameter.Trace == true)
                        {
                            Console.WriteLine(Tool.IsZh() ? "芯片ID获取失败，重试" : "Get chip ID fail, retry.");
                        }

                        //也许你看到这行代码的时候会感觉疑惑，这看起来是一个非常愚蠢的行为，让人无法理解。
                        //但是事实并不是这样，经过逻辑分析仪的抓取，我们发现使用CDC驱动的USB转串口，在Windows下使能RTS或者DTR似乎会发出一个奇怪的字节，
                        //这个字节可能是0x7F或者0xFD等，暂时还没找到什么规律。但是正因为串入了这个字节，因此mcu接收到的第一个字节就不是我们发送的用来握手的0x7F，
                        //这样后续的整个指令将会完全乱掉，因此我们额外添加了一个字节去处理，假如GetID操作失败的话，很有可能就是因为发送的指令乱掉了，那么我们手动
                        //加入一个字节来补全，并尝试重试3次。
                        Write(new byte[] { 0x7F },5); //这个操作可能不会返回任何有效字节，只是单纯写入，因此超时时间可以设置小一点
                    }
                    else
                    {
                        break;
                    }
                }
                
                return true;
            }
            resetEvent.Reset();
            Console.WriteLine("");
            Console.WriteLine(Tool.IsZh() ? "自动进入boot模式失败，操作超时，结束操作...\r\n" +
                "你可以尝试手动进入boot模式：按住BOOT按键不要松开，按一下RST复位，重新尝试下载操作\r\n" +
                "（直到下载成功前，都不要松开BOOT按键，下载完成后再松开，然后按一下RST复位）" : "fail to reset device to boot status, timeout, exit...");
            Environment.Exit(0);
            return false;
        }

        public static bool ResetAPP()
        {
            Console.WriteLine($"Leaving...");
            switch (baseParameter.After)
            {
                // 硬重启
                case "hard_reset":

                    switch (baseParameter.Before)
                    {
                        case "direct_connect":
                            serial.DtrEnable = true;
                            serial.RtsEnable = true;
                            Thread.Sleep(200);
                            serial.RtsEnable = false;

                            break;

                        case "default_reset":
                            serial.RtsEnable = true;

                            Thread.Sleep(10);
                            serial.RtsEnable = false;
                            break;
                    }

                    Console.WriteLine(Tool.IsZh() ? "通过RTS硬件复位..." : "Hard resetting via RTS pin...");
                    return true;

                default: return false;
            }
        }

        /// <summary>
        /// 向芯片中写入一系列数据，并检查是否有ACK
        /// </summary>
        /// <param name="serial"></param>
        /// <param name="data"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static bool Write(byte[] data, int timeOut = 200, int ACKCount = 1)
        {
            try
            {
                serial.Write(data, 0, data.Length);
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine($"There seems to be a problem with your serial device, please re-run the ISP software or replace the device and try again.");
                Console.WriteLine(ex.ToString());
            }

            int length;
            for (int j = 0; j < ACKCount; j++)
            {
                for (int i = 0; i < timeOut; i++)
                {
                    length = serial.BytesToRead;
                    if (length > 0)
                    {
                        var rev = new byte[length];
                        serial.Read(rev, 0, length);
                        if (baseParameter.Trace == true)
                        {
                            Console.WriteLine($"Retrieved data: {BitConverter.ToString(rev)}");
                        }
                        if (rev.Contains((byte)ReturnVal.ACK))
                        {
                            return true;
                        }

                    }
                    Thread.Sleep(1);
                }
            }
            return false;
        }

        /// <summary>
        /// 向芯片中写入一系列数据，并返回读取到的全部数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="timeOut"></param>
        /// <param name="ACKCount"></param>
        /// <returns></returns>
        public static byte[]? WriteAndRead(byte[] data, int timeOut = 200, int ACKCount = 1)
        {
            try
            {
                serial.Write(data, 0, data.Length);
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine($"There seems to be a problem with your serial device, please re-run the ISP software or replace the device and try again.");
                Console.WriteLine(ex.ToString());
            }

            int length;
            for (int i = 0; i < timeOut; i++)
            {
                length = serial.BytesToRead;
                if (length > 0)
                {
                    var rev = new byte[length];
                    serial.Read(rev, 0, length);
                    if (baseParameter.Trace == true)
                    {
                        Console.WriteLine($"Retrieved data: {BitConverter.ToString(rev)}");
                    }
                    return rev;
                }
                Thread.Sleep(1);
            }
            return null;
        }
    }
}
