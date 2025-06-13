using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{

    public class moduledata
    {
        public int ID { get; set; }
        public string Module { get; set; }
        public string SOLI { get; set; }
        public string Style { get; set; }
        public string cw { get; set; }
        public string cutb { get; set; }
        public int? inqty { get; set; }
        public string delivery { get; set; }
        public int? fgq { get; set; }
       

    }
}
