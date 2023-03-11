using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class ReadProtectClass
    {
        public static bool ReadProtect()
        {
            if (BasicOperation.baseParameter.Trace == true)
            {
                Console.WriteLine("Start turn on read protection...");
            }
            var data = new byte[] { (byte)BasicOperation.Command.ReadProtect, (byte)~BasicOperation.Command.ReadProtect };
            return BasicOperation.Write(data, 500, 2);//2次ACK
        }

        public static bool ReadProtectCommand()
        {
            BasicOperation.ResetBootloader();
            if (ReadProtectClass.ReadProtect() == false)
            {
                Console.WriteLine($"Enable read protect failed.");
                return false;
            }
            BasicOperation.ResetAPP();
            return true;
        }
    }
}
