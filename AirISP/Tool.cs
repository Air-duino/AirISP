using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirISP
{
    static class Tool
    {
        /// <summary>
        /// 把一个hex文件转换为一个byte[]类型的数据，并返回它的起始地址
        /// </summary>
        /// <param name="hexFilePath">hex文件的路径</param>
        /// <returns></returns>
        public static (string, byte[]) HexToBin(string hexFilePath)
        {
            // 读取hex文件的所有行
            string[] lines = File.ReadAllLines(hexFilePath);
            // 定义一个字符串变量来存储需要烧录的起始地址
            string startAddress = "-1";
            // 定义一个内存流来存储转换后的bin文件的字节流
            MemoryStream binStream = new MemoryStream();
            // 定义一个整数变量来记录当前的地址位置
            int currentAddress = 0;
            // 遍历每一行
            foreach (string line in lines)
            {
                // 去掉冒号和空格
                string hexLine = line.Replace(":", "").Replace(" ", "");
                // 获取数据区字节数
                int dataLength = Convert.ToInt32(hexLine.Substring(0, 2), 16);
                // 获取偏移地址或无用填0
                int offsetAddress = Convert.ToInt32(hexLine.Substring(2, 4), 16);
                // 获取记录类型
                int recordType = Convert.ToInt32(hexLine.Substring(6, 2), 16);
                // 根据记录类型进行不同的处理
                switch (recordType)
                {
                    case 0: // 数据记录，转换为二进制并写入内存流中
                        if (startAddress != null) // 如果已经获取到起始地址，才进行数据写入操作，否则忽略数据记录
                        {
                            if (currentAddress < offsetAddress) // 如果当前地址小于偏移地址，说明有间隔，用0xFF填充间隔区域
                            {
                                for (int i = currentAddress; i < offsetAddress; i++)
                                {
                                    binStream.WriteByte(0xFF);
                                }
                            }
                            for (int i = 0; i < dataLength; i++) // 将数据区转换为二进制并写入内存流中，并更新当前地址位置
                            {
                                byte dataByte = Convert.ToByte(hexLine.Substring(8 + i * 2, 2), 16);
                                binStream.WriteByte(dataByte);
                                currentAddress++;
                            }
                        }
                        break;
                    case 1: // 文件结束记录，直接返回结果
                        return (startAddress!, binStream.ToArray());
                    case 4: // 扩展线性地址记录，获取高16位地址并拼接为起始地址字符串，并重置当前地址位置为0x0000
                        string highAddress = hexLine.Substring(8, 4);
                        startAddress = "0x" + highAddress + "0000";
                        currentAddress = 0x0000;
                        break;
                    default: // 其他类型记录，忽略不处理
                        break;
                }
            }
            return (startAddress, binStream.ToArray());
        }

        private static bool? isZh = null;
        /// <summary>
        /// 判断是非为中文系统
        /// </summary>
        /// <returns></returns>
        public static bool IsZh()
        {
            if(isZh == null)
                isZh = System.Globalization.CultureInfo.CurrentCulture.Name.ToUpper().IndexOf("ZH") == 0;
            return isZh.Value;
        }
    }
}
