using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models.AMR
{
    [Table("KreedaIot_AMR")]
    public class KreedaIot_AMR
    {
        public int ID { get; set; }
        public string TRNO { get; set; }
        public string Module { get; set; }
        public string JobID { get; set; }
        public DateTime Date { get; set; }
        public string Next { get; set; }
        public string status { get; set; }
        public TimeSpan Time { get; set; }

        public string location { get; set; }
       

    }
}
