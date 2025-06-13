using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("KreedaIot_TackTime")]

    public class tack
    {
        public int ID { get; set; }
        public string Module { get; set; }
        public int Tack { get; set; }
     

    }
}
