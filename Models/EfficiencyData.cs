using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("KreedaIotTestNew")]
    public class KreedaIotTestNew
    {
        public int ID { get; set; }
        public int efficency { get; set; }
        public string Plant { get; set; }
        public int count { get; set; }
        public int performance { get; set; }
        public DateTime Date { get; set; }
        public string ChipID { get; set; }
        public string Module { get; set; }
        public int cycleAct { get; set; }
        public int cycleStand { get; set; }
        public int Hour { get; set; }
        public TimeSpan Time { get; set; }
        public string Operation { get; set; }
        public string MachineID { get; set; }
        public string UID { get; set; }
        public string UserName { get; set; }
    }
}
