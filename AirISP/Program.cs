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
            var before = new Option<string>("--before", () => "default_reset", "Specify the AirISP command to be preformed before execution.");
            var after = new Option<string>("--after", () => "hard_reset", "Specify the AirISP command to be preformed after execution.");
            rootCommand.Add(chip);
            rootCommand.Add(port);
            rootCommand.Add(baud);
            rootCommand.Add(trace);
            rootCommand.Add(connect_attempts);
            rootCommand.Add(before);
            rootCommand.Add(after);

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
            var readUnprotectCommand = new Command("read_unprotect", "Disables the read protection");

            readUnprotectCommand.SetHandler((baseParm) =>
            {
                BasicOperation.SetBaseParameter(baseParm);
                BasicOperation.Begin();

                ReadUnprotectClass.ReadUnprotectCommand();
            },
            new BinderBaseParameter(chip, port, baud, trace, connect_attempts, before, after));

            rootCommand.AddCommand(readUnprotectCommand);

            //创建启动读保护命令
            var readProtectCommand = new Command("read_protect", "Enables the read protection");

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
            return 0;
        }
    }
}