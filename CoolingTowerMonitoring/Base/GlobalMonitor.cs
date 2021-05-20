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
        //存储区列表
        public static List<StorageModel> StorageList { get; set; }
        public static List<DeviceModel> DeviceList { get; set; }
        public static SerialInfo SerialInfo { get; set; }

        static bool isRunning = true;
        static Task mainTask = null;
        static RTU rtuInstance = null;

        public static void Start(Action successAction, Action<string> faultAction)
        {
            mainTask = Task.Run(async () =>
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

                    int startAddr = 0;
                    while (isRunning)
                    {
                        foreach (var item in StorageList)
                        {
                            if (item.Length > 100)
                            {
                                startAddr = item.StartAddress;
                                int readCount = item.Length / 100;
                                for (int i = 0; i < readCount; i++)
                                {
                                    int readLen = i == readCount ? item.Length - 100 * i : 100;
                                    await rtuInstance.Send(item.SlaveAdress, (byte)Convert.ToInt32(item.FuncCode), startAddr + 100 * (item.Length / 100), item.Length % 100);
                                }
                            }
                            if (item.Length % 100 > 0)
                            {
                                await rtuInstance.Send(item.SlaveAdress, (byte)Convert.ToInt32(item.FuncCode), startAddr + 100 * (item.Length / 100), item.Length % 100);
                            }
                        }
                    }
                }
                else
                {
                    faultAction("串口连接初始化失败，请检查设备！");
                }

            });
        }

        /// <summary>
        /// 查找设备监控点位与当前返回报文相关的监控数据列表
        /// byteList[0]：从站地址
        /// byteList[1]：功能码
        /// startAddr：起始地址
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="byteList"></param>
        private static void ParsingData(int startAddr, List<byte> byteList)
        {
            if (byteList != null && byteList.Count > 0)
            {
                var mvl = (from q in DeviceList
                           from m in q.MonitorValueList
                           where m.StorageAreaId == (byteList[0].ToString() + byteList[1].ToString("00") + startAddr.ToString()) && q.IsRuning
                           select m).ToList();

                int startByte;
                byte[] res = null;
                foreach (var item in mvl)
                {
                    switch (item.DataType)
                    {
                        case "Float":
                            startByte = item.StartAdress * 2 + 3;
                            res = new byte[4] { byteList[startByte], byteList[startByte + 1], byteList[startByte + 2], byteList[startByte + 3] };
                            item._CurrentValue = Convert.ToDouble(res.ByteArraysToFloat());
                            break;
                        case "Bool":
                            break;
                        default:
                            break;
                    }
                }
            }
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
