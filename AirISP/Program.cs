using System.Diagnostics;
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
        static int Main(string[] args)
        {
            //防止控制台乱码
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 创建根命令
            var rootCommand = new RootCommand(
                Tool.IsZh() ? 
                "AirISP 是一个flash烧录工具" : 
                "AirISP is a tool for flash programming.");

            // 添加通用参数
            var chip = new Option<string>("--chip", Tool.IsZh() ? "目标芯片型号，auto/air001" : "Target chip type, optional auto,air001");
            chip.AddAlias("-c");
            var port = new Option<string>("--port", Tool.IsZh() ? "串口名称" : "Serial port device");
            port.AddAlias("-p");
            var baud = new Option<int>("--baud", Tool.IsZh() ? "串口波特率" : "Serial port baud rate used when flashing/reading");
            baud.AddAlias("-b");
            var trace = new Option<bool>("--trace", () => false, Tool.IsZh() ? "启用trace日志输出" : "Enable trace-level output of AirISP interactions.");
            trace.AddAlias("-t");
            var connect_attempts = new Option<int>("--connect-attempts", () => 10, Tool.IsZh() ? "最大重试次数，小于等于0表示无限次，默认为10次" : "Number of attempts to connect, negative or 0 for infinite. Default: 10");
            var before = new Option<string>("--before", () => "default_reset", Tool.IsZh() ? "下载前要执行的操作" : "Specify the AirISP command to be preformed before execution.");
            var after = new Option<string>("--after", () => "hard_reset", Tool.IsZh() ? "下载后要执行的操作" : "Specify the AirISP command to be preformed after execution.");
            rootCommand.Add(chip);
            rootCommand.Add(port);
            rootCommand.Add(baud);
            rootCommand.Add(trace);
            rootCommand.Add(connect_attempts);
            rootCommand.Add(before);
            rootCommand.Add(after);

            // 创建 chip_id 命令
            var chipIDCommand = new Command("chip_id", Tool.IsZh() ? "获取芯片ID" : "Get chip id");
            chipIDCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                GetClass.GetID();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));
            rootCommand.AddCommand(chipIDCommand);

            // 创建 get 命令
            var getCommand = new Command("get", Tool.IsZh() ? "获取ISP版本和支持的命令列表" : "Get the current ISP program version and the allowed commands");
            getCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                GetClass.Get();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));
            rootCommand.AddCommand(getCommand);

            // 创建 get_version 命令
            var getVersionCommand = new Command("get_version", Tool.IsZh() ? "获取ISP版本和芯片读保护状态" : "Get the ISP program version and the read protection status of Flash");
            getVersionCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                GetClass.GetVersionAndReadProtectionStatus();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));
            rootCommand.AddCommand(getVersionCommand);

            // 创建 write_flash 命令
            var writeFlashCommand = new Command("write_flash", Tool.IsZh() ? "向flash刷入固件" : "Write firmware to flash");
            // 添加命令参数
            var writeFlashAddress = new Argument<string>(name: "address", description: "0x00");
            var writeFlashFilename = new Argument<string>("filename");

            // 添加命令选项
            var writeFlashEarseAll = new Option<bool>("--erase-all", Tool.IsZh() ? "全片擦除（默认只擦除待写入的页）" : "Erase all sectors on flash before writing (default only erase sectors to be written)");
            writeFlashEarseAll.AddAlias("-e");
            var writeFlashNoProgress = new Option<bool>("--no-progress", Tool.IsZh() ? "禁止显示下载进度条" : "Disable progress bar printing");
            writeFlashNoProgress.AddAlias("-p");

            writeFlashCommand.Add(writeFlashAddress);
            writeFlashCommand.Add(writeFlashFilename);
            writeFlashCommand.Add(writeFlashEarseAll);
            writeFlashCommand.Add(writeFlashNoProgress);

            // 设置 write_flash 命令的处理器
            writeFlashCommand.SetHandler((baseParm,writeFlashParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                WriteFlashClass.SetWriteFlashParameter(writeFlashParm);
                WriteFlashClass.WriteFlash();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after),
            new BinderWriteFlashParameter(writeFlashAddress, writeFlashFilename, writeFlashEarseAll, writeFlashNoProgress));
            // 将 write_flash 命令添加到根命令中
            rootCommand.AddCommand(writeFlashCommand);

            //创建解除读保护命令
            var readUnprotectCommand = new Command("read_unprotect", Tool.IsZh() ? "关闭读保护" : "Disables the read protection");

            readUnprotectCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                ReadUnprotectClass.ReadUnprotectCommand();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));

            rootCommand.AddCommand(readUnprotectCommand);

            //创建启动读保护命令
            var readProtectCommand = new Command("read_protect", Tool.IsZh() ? "开启读保护" : "Enables the read protection");

            readProtectCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                ReadProtectClass.ReadProtectCommand();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));

            rootCommand.AddCommand(readProtectCommand);

            // 解析并执行命令行参数
            rootCommand.InvokeAsync(args);

            BasicOperation.End();

            if (Tool.IsZh())
                ColorfulConsole.InfoLine("【提示】\r\n" +
                    "若遇到因为AirMCU库或工具造成的BUG，请务必上报到下面的网址，以供开发者知晓该问题，并持续跟踪：\r\n" +
                    "https://github.com/Air-duino/Arduino-AirMCU/issues");
            return 0;
        }
    }
}