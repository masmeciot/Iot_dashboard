using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models.AMR
{
    [Table("KreedaAMR_order")]
    public class order
    {
        public int ID { get; set; }
        public string orderID { get; set; }
        public string status { get; set; }
        public string fail { get; set; }
        public string vehicle { get; set; }
        public string start { get; set; }
        public string endt { get; set; }
        public string compt { get; set; }
       


    }
}
