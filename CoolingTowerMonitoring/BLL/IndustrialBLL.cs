using Communication;
using CoolingTowerMonitoring.DAL;
using CoolingTowerMonitoring.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.BLL
{
    public class IndustrialBLL
    {
        DataAccess dataAccess = new DataAccess();

        /// <summary>
        /// 获取串口信息
        /// </summary>
        /// <returns></returns>
        public DataResult<SerialInfo> InitSerialInfo()
        {
            DataResult<SerialInfo> result = new DataResult<SerialInfo>();
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

        public DataResult<List<StorageModel>> InitStorageArea()
        {
            DataResult<List<StorageModel>> result = new DataResult<List<StorageModel>>();
            try
            {
                StorageModel storageModel = new StorageModel();
                var sa = dataAccess.GetStorageArea();
                result.State = true;
                result.Data = (from q in sa.AsEnumerable()
                               select new StorageModel
                               {
                                   Id = q.Field<string>("id"),
                                   SlaveAdress = q.Field<Int32>("slave_add"),
                                   StartAddress = int.Parse(q.Field<string>("start_reg")),
                                   FuncCode = q.Field<string>("func_code"),
                                   Length = int.Parse(q.Field<string>("length"))
                               }).ToList();
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public DataResult<List<DeviceModel>> InitDevices()
        {
            DataResult<List<DeviceModel>> result = new DataResult<List<DeviceModel>>();
            try
            {
                var devices = dataAccess.GetDevices();
                var monitorValues = dataAccess.GetMonitorValues();

                List<DeviceModel> deviceModels = new List<DeviceModel>();
                foreach (var item in devices.AsEnumerable())
                {
                    DeviceModel dModel = new DeviceModel();
                    deviceModels.Add(dModel);
                    dModel.DeviceId = item.Field<string>("d_id");
                    dModel.DeviceName = item.Field<string>("d_name");


                    foreach (var mv in monitorValues.AsEnumerable())
                    {
                        MonitorValueModel mvm = new MonitorValueModel();
                        dModel.MonitorValueList.Add(mvm);

                        mvm.ValueId = mv.Field<string>("value_id");
                        mvm.ValueName = mv.Field<string>("value_name");
                        mvm.ValuesDesc = mv.Field<string>("description");
                        mvm.StorageAreaId = mv.Field<string>("s_area_id");
                        mvm.StartAdress = mv.Field<Int32>("address");
                        mvm.DataType = mv.Field<string>("data_type");
                        mvm.IsAlarm = mv.Field<Int32>("is_alarm") == 1;
                        mvm.Unit = mv.Field<string>("unit");

                        //警戒值
                        var column = mv.Field<string>("alarm_lolo");
                        mvm.LoLoAlarm = column == null ? 0.0 : double.Parse(column);
                        column = mv.Field<string>("alarm_low");
                        mvm.LowAlarm = column == null ? 0.0 : double.Parse(column);
                        column = mv.Field<string>("alarm_hight");
                        mvm.HightAlarm = column == null ? 0.0 : double.Parse(column);
                        column = mv.Field<string>("alarm_hihi");
                        mvm.HiHiAlarm = column == null ? 0.0 : double.Parse(column);

                        mvm.ValueStateChanged = (state, msg, value_id) =>
                        {
                            var index = dModel.WarningMessageList.ToList().FindIndex(w => w.ValueId == value_id);
                            if (index > -1)
                            {
                                dModel.WarningMessageList.RemoveAt(index);
                            }
                            if (state != Base.MonitorValueStateEnum.OK)
                            {
                                dModel.IsWarning = true;
                                dModel.WarningMessageList.Add(new WarningMessageModel { ValueId = value_id, Message = msg });
                            }
                            var ss = dModel.WarningMessageList.Count > 0;
                            if (dModel.IsWarning != ss)
                            {
                                dModel.IsWarning = ss;
                            }
                        };
                    }
                }

                result.State = true;
                result.Data = deviceModels;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }
    }
}
