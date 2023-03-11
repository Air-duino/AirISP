using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    public class BaseParameter //命令前的参数
    {
        public string Chip { get; set; } = "auto"; //芯片型号
        public string Port { get; set; } = "COM0"; //选择的串口端口
        public int Baud { get; set; } = 115200; //波特率
        public bool Trace { get; set; } = false; //是否打开AirISP的所有操作细节
        public int ConnectAttempts { get; set; } = 10; //尝试连接次数，默认 10
        public string Before { get; set; } = "default_reset"; //指定芯片是否需要在 AirISP 其它命令执行之前重置为引导加载程序模式
        public string After { get; set; } = "hard_reset"; //指定芯片是否需要在 AirISP 操作完成后重置芯片
    }

    public class WriteFlashParameter //write_flash的参数
    {
        public string Address { get; set; } = "0";//需要下载的地址
        public string Filename { get; set; } = "b.bin";//需要下载的文件名

        public bool EraseAll { get; set; } = false;//在写固件时，擦除所有 flash 上所有扇区
        public bool NoProgress { get; set; } = false;//禁用进度条打印
    }

    public class BinderBaseParameter : BinderBase<BaseParameter>
    {
        private readonly Option<string> _chip;
        private readonly Option<string> _port;
        private readonly Option<int> _baud;
        private readonly Option<bool> _trace;
        private readonly Option<int> _connectAttempts;
        private readonly Option<string> _before;
        private readonly Option<string> _after;

        public BinderBaseParameter(Option<string> chip, Option<string> port, Option<int> baud, Option<bool> trace, Option<int> connectAttempts,Option<string> before, Option<string> after)
        {
            _chip = chip;
            _port = port;
            _baud = baud;
            _trace = trace;
            _connectAttempts = connectAttempts;
            _before = before;
            _after = after;
        }

        protected override BaseParameter GetBoundValue(BindingContext bindingContext) =>
            new BaseParameter
            {
                Chip = bindingContext.ParseResult.GetValueForOption(_chip),
                Port = bindingContext.ParseResult.GetValueForOption(_port),
                Baud = bindingContext.ParseResult.GetValueForOption(_baud),
                Trace = bindingContext.ParseResult.GetValueForOption(_trace),
                ConnectAttempts = bindingContext.ParseResult.GetValueForOption(_connectAttempts),
                Before = bindingContext.ParseResult.GetValueForOption(_before),
                After = bindingContext.ParseResult.GetValueForOption(_after)
            };
    }

    public class BinderWriteFlashParameter : BinderBase<WriteFlashParameter>
    {
        private readonly Argument<string> _address;
        private readonly Argument<string> _filename;
        private readonly Option<bool> _eraseAll;
        private readonly Option<bool> _noProgress;

        public BinderWriteFlashParameter(Argument<string> address, Argument<string> filename, Option<bool> eraseAll, Option<bool> noProgress)
        {
            _address = address;
            _filename = filename;
            _eraseAll = eraseAll;
            _noProgress = noProgress;
        }

        protected override WriteFlashParameter GetBoundValue(BindingContext bindingContext) =>
            new WriteFlashParameter
            {
                Address = bindingContext.ParseResult.GetValueForArgument(_address),
                Filename = bindingContext.ParseResult.GetValueForArgument(_filename),

                EraseAll = bindingContext.ParseResult.GetValueForOption(_eraseAll),
                NoProgress = bindingContext.ParseResult.GetValueForOption(_noProgress)
            };
    }
}
