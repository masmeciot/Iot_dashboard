using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("IoTdash")]

    public class iotdash
    {
        public string UserName { get; set; }
        public string Module { get; set; }
     
        public int Hour { get; set; }

        public DateTime Date { get; set; }


        public int Hand { get; set; }
        public TimeSpan Time { get; set; }
        public int Sew { get; set; }
        public string MachineID { get; set; }
        public string style { get; set; }
        public string Plant { get; set; }
                public string Operation { get; set; }
        public string ChipID { get; set; }

        public int TotalRuntime { get; set; }
        public int DeltaTime { get; set; }

    }
}
