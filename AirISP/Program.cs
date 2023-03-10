﻿using System.Diagnostics;
using System.IO.Ports;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using System.IO;

namespace AirISP
{
    internal class Program
    {
        public struct BaseParameter //命令前的参数
        {
            public string chip;
            public string port;
            public string baud;
            public bool trace;
            public int connectAttempts;
        }

        public struct WriteFlashParameter //write_flash的参数
        {
            public string address;
            public string filename;
        }

        static int Main(string[] args)
        {
            // 创建根命令
            var rootCommand = new RootCommand("AirISP is a tool for flash programming.");

            // 添加通用参数
            var chip = new Option<string>("--chip", "Target chip type, optional auto,air001");
            chip.AddAlias("-c");
            var port = new Option<string>("--port", "Serial port device");
            port.AddAlias("-p");
            var baud = new Option<int>("--baud", "Serial port baud rate used when flashing/reading");
            baud.AddAlias("-b");
            var trace = new Option<bool>("--trace", () => false, "Enable trace-level output of AirISP interactions.");
            trace.AddAlias("-t");
            var connect_attempts = new Option<int>("--connect-attempts", () => 10, "Number of attempts to connect, negative or 0 for infinite. Default: 10");
            rootCommand.Add(chip);
            rootCommand.Add(port);
            rootCommand.Add(baud);
            rootCommand.Add(trace);
            rootCommand.Add(connect_attempts);

            // 创建 write_flash 命令
            var writeFlashCommand = new Command("write_flash", "Write firmware to flash");
            // 添加命令参数
            var writeFlashAddress = new Argument<string>(name: "address", description: "0x00");
            var writeFlashFilename = new Argument<string>("filename");

            // 添加命令选项
            var writeFlashEarseAll = new Option<bool>("--erase-all", "Erase all sectors on flash before writing (default only erase sectors to be written)");
            writeFlashEarseAll.AddAlias("-e");
            var writeFlashNoProgress = new Option<bool>("--no-progress", "Disable progress bar printing");
            writeFlashNoProgress.AddAlias("-p");

            writeFlashCommand.Add(writeFlashAddress);
            writeFlashCommand.Add(writeFlashFilename);
            writeFlashCommand.Add(writeFlashEarseAll);
            writeFlashCommand.Add(writeFlashNoProgress);

            //rootCommand.AddCommand(writeFlashCommand);

            // 设置 write_flash 命令的处理器
            writeFlashCommand.SetHandler((baseParm, writeFlashParm) =>
            {
                //TODO
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts),
            new BinderWriteFlashParameter(writeFlashAddress, writeFlashFilename, writeFlashEarseAll, writeFlashNoProgress));

            // 将 write_flash 命令添加到根命令中
            rootCommand.AddCommand(writeFlashCommand);

            // 解析并执行命令行参数
            rootCommand.InvokeAsync(args);


            return 0;
        }

        public static bool WriteFlash(string chip, string serialPort, int baud, bool trace, int connect_attempts, string address, string filename, bool earseAll, bool noProgress)
        {
            Console.WriteLine($"AirISP v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine($"Serial port {serialPort}");
            Console.WriteLine("Connect...");

            //读取固件数据
            var data = File.ReadAllBytes(address[0]);

            //打开串口
            var port = new SerialPort(serialPort, baud, Parity.Even, 8, StopBits.One);
            port.Open();

            if (port.IsOpen == false)
            {
                Console.WriteLine("Serial port opening failed!!!");
                return false;
            }

            //重启设备
            var resetResult = false;
            for (int i = 0; i < connect_attempts; i++)
            {
                Console.WriteLine($"try to reset device via rts and dtr ({i + 1}/10) ...");
                if (TryReset(port, true))
                {
                    resetResult = true;
                    break;
                }
            }
            //自动重启失败，等待手动重启进boot
            if (!resetResult)
            {
                Console.WriteLine($"fail to reset device to boot status, please reset device manually ...");
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine($"waitting ({i + 1}/10) ...");
                    if (TryReset(port))
                    {
                        resetResult = true;
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }
            //手动重启进boot也没成功
            if (!resetResult)
            {
                Console.WriteLine($"fail to reset device to boot status, timeout, exit");
                return false;
            }

            //擦除数据
            Console.WriteLine($"try erase flash ...");
            //试试看能不能进入擦除模式
            if (!retry(port, new byte[] { 0x44, 0xbb }, 0x79, 5, 100))
            {
                Thread.Sleep(200);
                //进不去，可能读保护开了，关掉
                Console.WriteLine($"{retry(port, new byte[] { 0x92, 0x6d }, 0x79, 5, 100)}");
                Console.WriteLine("enter erase mode failed, please retry later");
                return false;
            }
            //全擦地址
            if (retry(port, new byte[] { 0xff, 0xff, 0x00 }, 0x79, 1, 100))
                Thread.Sleep(2000);
            else
            {
                Console.WriteLine("erase failed, please retry later");
                return false;
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
            var now = baseAddress;//现在的地址
            var latestAddr = now + data.Length;//固件长度
            while (now < latestAddr)
            {
                if (!retry(port, new byte[] { 0x31, 0xCE }, 0x79, 1, 50))
                {
                    Console.WriteLine($"prepare writing to address 0x{now:X} failed.");
                    return 4;
                }
                var addrBytes = new byte[] {
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
                if (!retry(port, addrBytes, 0x79, 1, 50))
                {
                    Console.WriteLine($"set write address 0x{now:X} failed.");
                    return 4;
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
                if (!retry(port, flashData, 0x79, 1, 500))
                {
                    Console.WriteLine($"writing to address 0x{now:X} failed.");
                    return 4;
                }

                now += 0x80;
                Console.WriteLine($"downloading... {(double)(now - baseAddress) / (latestAddr - baseAddress) * 100:f2}%");
            }


            //正常重启进入运行模式
            Console.WriteLine($"all done, reboot.");
            port.DtrEnable = true;
            retry(port, new byte[] { 0x21, 0xde }, 0x79, 5, 100);
            retry(port, new byte[] { 0x08, 0, 0, 0, 0x08 }, 0x79, 1, 0);
            port.RtsEnable = true;
            Thread.Sleep(200);
            port.RtsEnable = false;
            Thread.Sleep(200);
        }

        /// <summary>
        /// 重启进入boot模式
        /// </summary>
        /// <param name="port">串口对象</param>
        /// <param name="uart_ctrl">是否使用硬件流控进入重启？rts#连reset，dtr#连boot</param>
        /// <returns></returns>
        public static bool TryReset(SerialPort port, bool uart_ctrl = false)
        {
            //串口芯片控制尝试进入boot模式
            if (uart_ctrl)
            {
                port.DtrEnable = false;
                port.RtsEnable = true;
                Thread.Sleep(200);
                port.RtsEnable = false;
                Thread.Sleep(100);
            }
            return retry(port, new byte[] { 0x7f }, 0x79, 5, 100);
        }

        public static bool retry(SerialPort serial, byte[] data, byte exp, int count, int wait)
        {
            for (int i = 0; i < count; i++)
            {
                serial.Write(data, 0, data.Length);
                int length;
                for (int c = 0; c < wait; c++)//尽量快吧
                {
                    length = serial.BytesToRead;
                    if (length > 0)
                        break;
                    Thread.Sleep(1);
                }
                length = serial.BytesToRead;
                if (length > 0)
                {
                    byte[] rev = new byte[length];
                    serial.Read(rev, 0, length);
                    Debug.WriteLine($"{BitConverter.ToString(rev)}");
                    if (rev.Contains(exp))
                        return true;
                }

                //可能错开了1字节，补上试试
                serial.Write(new byte[] { 0x00 }, 0, 1);
            }
            return false;
        }
    }
}