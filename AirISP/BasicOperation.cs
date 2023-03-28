using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine($"Serial port {baseParameter.Port}");
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
                    Console.WriteLine($"Failed to open serial port: {ex.Message}");
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
            Console.WriteLine("Connect...");
            for (int i = 0; i < baseParameter.ConnectAttempts; i++)
            {
                switch (baseParameter.Before)
                {
                    //DTR连接BOOT0，RTS连接RST
                    case "direct_connect":
                        serial.RtsEnable = true;
                        serial.DtrEnable = false;
                        Thread.Sleep(200);
                        serial.RtsEnable = false;

                        if (Write(new byte[] { 0x7F }, 100) == true)
                        {
                            Console.WriteLine($"Connect success.");
                            return true;
                        }
                        break;

                        // 采用异或电路
                    case "default_reset":
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

                        if (Write(new byte[] { 0x7F }, 100) == true)
                        {
                            Console.WriteLine($"Connect success.");
                            return true;
                        }

                        break;

                    default: return false;
                }
            }
            Console.WriteLine($"fail to reset device to boot status, timeout, exit");
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

                    switch(baseParameter.Before)
                    {
                        case "direct_connect":
                            serial.DtrEnable = true;
                            serial.RtsEnable = true;
                            Thread.Sleep(200);
                            serial.RtsEnable = false;
                            
                            break;

                        case "default_reset":
                            serial.RtsEnable= true;

                            Thread.Sleep(10);
                            serial.RtsEnable = false;
                            break;
                    }

                    Console.WriteLine("Hard resetting via RTS pin...");
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
                            return true;
                    }
                    Thread.Sleep(1);
                }
            }
            try
            {
                serial.Write(new byte[] { 0x00 }, 0, 1);
            }
            catch (System.IO.IOException ex)
            {
                Console.WriteLine($"There seems to be a problem with your serial device, please re-run the ISP software or replace the device and try again.");
                Console.WriteLine(ex.ToString());
            }
            return false;
        }
    }
}
