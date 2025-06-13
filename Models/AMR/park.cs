using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models.AMR
{
    [Table("KreedaIot_ParkSlots")]
    public class park
    {
        public int ID { get; set; }
        public string Slot { get; set; }
        public string Category { get; set; }
        public string status { get; set; }
      
       

    }
}
