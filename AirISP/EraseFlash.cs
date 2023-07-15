using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class EraseFlashClass
    {
        public static bool EraseFlash()
        {
            Console.WriteLine(Tool.IsZh() ? "擦除flash中（请耐心等待）..." : "Erasing flash (this may take a while)...");
            var nowTime = DateTime.Now;
            //试试看能不能进入擦除模式
            var data = new byte[2] { (byte)BasicOperation.Command.ExtendedErase, (byte)~BasicOperation.Command.ExtendedErase };
            if (BasicOperation.Write(data, 500) == false)
            {
                //进不去，可能读保护开了，关掉
                if (ReadUnprotectClass.ReadUnprotect() == false)
                {
                    return false;
                }
                return EraseFlash();
            }
            //全擦地址
            data = new byte[] {0xFF,0xFF,0x00 };
            if (BasicOperation.Write(data,500) == true)
            {
                Console.WriteLine(Tool.IsZh() ?
                    $"擦除成功，耗时 {DateTime.Now.Subtract(nowTime).TotalMilliseconds} ms." : 
                    $"Erase flash sucess, in {DateTime.Now.Subtract(nowTime).TotalMilliseconds} ms.");
                return true;
            }
            else
            {
                Console.WriteLine(Tool.IsZh() ? "擦除失败，请重试" : "Erase failed, please retry later");
                return false;
            }
        }
    }
}
