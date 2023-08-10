using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class ReadUnprotectClass
    {
        public static bool ReadUnprotect()
        {
            if (BasicOperation.baseParameter.Trace == true)
            {
                ColorfulConsole.LogLine("Start turn off read protection...");
                ColorfulConsole.WarnLine("this will erase all content on your flash !!!");
            }
            var data = new byte[] { (byte)BasicOperation.Command.ReadUnrotect, (byte)~BasicOperation.Command.ReadUnrotect };
            return BasicOperation.Write(data, 500, 2);//2次ACK
        }

        public static bool ReadUnprotectCommand()
        {
            BasicOperation.ResetBootloader();
            if (ReadUnprotectClass.ReadUnprotect() == false)
            {
                ColorfulConsole.WarnLine($"Disable read protect failed.");
                return false;
            }
            BasicOperation.ResetAPP();
            return true;
        }
    }
}
