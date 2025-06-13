using System.ComponentModel.DataAnnotations;

namespace Iot_dashboard.Models
{
    public class zone
    {
        public int ID { get; set; }

        [Required]
        public string Zone { get; set; }
        [Required]
        public int Stich { get; set; }
        [Required]
        public string Operation { get; set; }
        [Required]
        public int Wait { get; set; }
        [Required]
        public string style { get; set; }
        [Required]
        public string operationZone { get; set; }

        public string Plant { get; set; }
        
        public string UserName { get; set; }

    }
}
