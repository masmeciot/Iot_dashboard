using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{

    public class KreedaIotUser
    {
        public int ID { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string plant { get; set; }


    }
}
