using System.ComponentModel.DataAnnotations;

namespace Iot_dashboard.Models
{
    public class style
    {
        public int ID { get; set; }
        [Required]
        public string Style { get; set; }
        [Required]
        public string Operation { get; set; }
        [Required]
        public string Users { get; set; }

    }
}
