using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    /// <summary>
    /// 一系列的Get命令
    /// </summary>
    static class GetClass
    {
        /// <summary>
        /// 用户通过 Get 命令可获取IAP程序的版本及支持的命令
        /// </summary>
        /// <returns></returns>
        public static bool Get()
        {
            if (BasicOperation.baseParameter.Trace == true)
            {
                ColorfulConsole.LogLine("Start getting the version of the ISP program and the supported commands.");
            }
            var data = new byte[] { (byte)BasicOperation.Command.Get, (byte)~BasicOperation.Command.Get };
            var returnVal =  BasicOperation.WriteAndRead(data);
            if(returnVal == null)
            {
                return false;
            }
            if (returnVal[0] == (byte)BasicOperation.ReturnVal.NACK)
            {
                return false;
            }
            var byteNum = returnVal[1];
            var ISPVersion = BitConverter.ToString(returnVal, 2,1).Replace("-", "").ToCharArray(); ;
            ColorfulConsole.InfoLine($"ISP Version is v{ISPVersion[0]}.{ISPVersion[1]}.");
            ColorfulConsole.InfoLine("ISP supports the following commands.");
            for( int i = 0; i < byteNum; i++ )
            {
                ColorfulConsole.InfoLine($"0x{returnVal[3+i]:X}");
            }
            return true;
        }

        public static bool GetVersionAndReadProtectionStatus()
        {
            if (BasicOperation.baseParameter.Trace == true)
            {
                ColorfulConsole.LogLine("Start get version and read protection status.");
            }
            var data = new byte[] { (byte)BasicOperation.Command.GetVersion, (byte)~BasicOperation.Command.GetVersion };
            var returnVal = BasicOperation.WriteAndRead(data);
            if (returnVal == null)
            {
                return false;
            }
            if (returnVal[0] == (byte)BasicOperation.ReturnVal.NACK)
            {
                return false;
            }
            var ISPVersion = BitConverter.ToString(returnVal, 1, 1).Replace("-", "").ToCharArray(); ;
            ColorfulConsole.InfoLine($"ISP Version is v{ISPVersion[0]}.{ISPVersion[1]}.");
            return true;
        }

        public static bool GetID()
        {
            if (BasicOperation.baseParameter.Trace == true)
            {
                ColorfulConsole.LogLine("Start get chip ID.");
            }
            var data = new byte[] { (byte)BasicOperation.Command.GetID, (byte)~BasicOperation.Command.GetID };
            var returnVal = BasicOperation.WriteAndRead(data);
            if (returnVal == null)
            {
                return false;
            }
            else if (returnVal[0] == (byte)BasicOperation.ReturnVal.NACK)
            {
                return false;
            }

            if(returnVal.Length < 2)
                return false;
            var byteNum = returnVal[1] + 1;
            if (byteNum != returnVal.Length-3)
            {
                return false;
            }
            ColorfulConsole.Info("Chip PID is: ");
            for (int i = 0;i<byteNum;i++)
            {
                ColorfulConsole.Info($"0x{returnVal[i + 2]:X2} ");
            }
            ColorfulConsole.LogLine("");
            return true;
        }
    }
}
