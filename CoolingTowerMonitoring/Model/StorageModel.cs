using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolingTowerMonitoring.Model
{
    public class StorageModel
    {
        public string Id { get; set; }
        public int SlaveAdress { get; set; }
        public string FuncCode { get; set; }
        public int StartAddress { get; set; }
        public int Length { get; set; }
    }
}
