using Communication;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.BLL
{
    public class IndustrialBLL
    {
        /// <summary>
        /// 获取串口信息
        /// </summary>
        /// <returns></returns>
        public DataResult<SerialInfo> InitSerialInfo()
        {
            DataResult<SerialInfo> result = new DataResult<SerialInfo>();
            result.State = false;
            try
            {
                SerialInfo serialInfo = new SerialInfo();
                serialInfo.PortName = ConfigurationManager.AppSettings["port"].ToString();
                serialInfo.BaudRate = Convert.ToInt32(ConfigurationManager.AppSettings["baud"].ToString());
                serialInfo.DataBit = Convert.ToInt32(ConfigurationManager.AppSettings["data_bit"].ToString());
                serialInfo.Parity = (Parity)Enum.Parse(typeof(Parity), ConfigurationManager.AppSettings["data_bit"].ToString(), true);
                serialInfo.StopBits = (StopBits)Enum.Parse(typeof(StopBits), ConfigurationManager.AppSettings["stop_bit"].ToString(), true);
                result.State = true;
                result.Data = serialInfo;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
