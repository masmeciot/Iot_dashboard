using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("KreedaIotTestNew")]
    public class iotplusmodel
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string ChipID { get; set; }

        [Required]
        public string MAC { get; set; }

        [Required]
        public string Module { get; set; }

        [Required]
        public string Operation { get; set; }

        [Required]
        public string Plant { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string IP { get; set; }

        [Required]
        public string UID { get; set; }

        [Required]
        public string GatewayIp { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        public string MachineID { get; set; }

        [Required]
        public string style { get; set; }

        [Required]
        public string Shift { get; set; }

        [Required]  
        public int RequestID { get; set; }
    }
}
