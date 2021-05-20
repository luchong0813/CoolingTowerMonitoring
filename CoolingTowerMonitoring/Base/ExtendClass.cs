using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.Base
{
    public static class ExtendClass
    {
        /// <summary>
        /// Byte数组转Float
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static float ByteArraysToFloat(this byte[] data)
        {
            float val = 0x0f;
            uint rest = ((uint)data[2] * 256) + ((uint)data[3]) + 65536 * ((uint)data[0] * 256 + ((uint)data[1]));
            unsafe
            {
                float* temp;
                temp = (float*)(&rest);
                val = *temp;
            }
            return val;
        }
    }
}
