using CoolingTowerMonitoring.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.Model
{
    public class MonitorValueModel
    {
        public Action<MonitorValueStateEnum, string, string> ValueStateChanged;
        public string ValueId { get; set; }
        public string ValueName { get; set; }
        public string StorageAreaId { get; set; }
        public int StartAdress { get; set; }
        public string DataType { get; set; }
        public bool IsAlarm { get; set; }
        public double LoLoAlarm { get; set; }
        public double LowAlarm { get; set; }
        public double HightAlarm { get; set; }
        public double HiHiAlarm { get; set; }
        public string Unit { get; set; }

        private double _currentValue;

        public double _CurrentValue
        {
            get { return _currentValue; }
            set
            {
                _currentValue = value;
                if (IsAlarm)
                {
                    string msg = ValuesDesc;
                    MonitorValueStateEnum state = MonitorValueStateEnum.OK;

                    if (value < LoLoAlarm) { msg += "极低"; state = MonitorValueStateEnum.LoLo; }
                    else if (value < LowAlarm) { msg += "过低"; state = MonitorValueStateEnum.Low; }
                    else if (value > HiHiAlarm) { msg += "极高"; state = MonitorValueStateEnum.HiHi; }
                    else if (value > HightAlarm)
                    {
                        msg += "过高";
                        state = MonitorValueStateEnum.High;
                    }
                    ValueStateChanged(state, $"{msg}。当前值：{value}", ValueId);
                }
            }
        }

        public string ValuesDesc { get; set; }

    }
}
