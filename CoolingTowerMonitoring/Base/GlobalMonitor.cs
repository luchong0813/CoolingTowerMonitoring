using Communication;
using Communication.Modbus;
using CoolingTowerMonitoring.BLL;
using CoolingTowerMonitoring.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.Base
{
    public class GlobalMonitor
    {
        public static List<StorageModel> StorageList { get; set; }
        public static List<DeviceModel> DeviceList { get; set; }
        public static SerialInfo SerialInfo { get; set; }

        static bool isRunning = true;
        static Task mainTask = null;
        static RTU rtuInstance = null;

        public static void Start(Action successAction, Action<string> faultAction)
        {
            mainTask = Task.Run(() =>
            {
                IndustrialBLL bll = new IndustrialBLL();
                //获取串口配置信息
                var si = bll.InitSerialInfo();
                if (si.State)
                {
                    SerialInfo = si.Data;
                }
                else
                {
                    faultAction(si.Message);
                    return;
                }

                //获取存储区信息
                var sa = bll.InitStorageArea();
                if (sa.State)
                    StorageList = sa.Data;
                else
                {
                    faultAction(sa.Message);
                    return;
                }

                //初始化设备变量及警戒值
                var dr = bll.InitDevices();
                if (dr.State)
                    DeviceList = dr.Data;
                else
                {
                    faultAction(dr.Message);
                    return;
                }

                rtuInstance = RTU.GetInstance(SerialInfo);
                if (rtuInstance.Connection())
                {
                    successAction();
                    while (isRunning)
                    {

                    }
                }
                else
                {
                    faultAction("串口连接初始化失败，请检查设备！");
                }

            });
        }

        public static void Disponse()
        {
            isRunning = false;
            if (rtuInstance != null)
            {
                rtuInstance.Dispose();
            }
            if (mainTask != null)
            {
                mainTask.Wait();
            }
        }
    }
}
