using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("KreedIot_UserSMV")]

    public class UserSMV
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public int Hand { get; set; }
        public int sew { get; set; }
        public string style { get; set; }
        public string Plant { get; set; }
        public string Operation { get; set; }
        public string Module { get; set; }
    }
}
