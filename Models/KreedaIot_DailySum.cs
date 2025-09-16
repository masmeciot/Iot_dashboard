using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    public class KreedaIot_DailySum
    {
        [Key]
        public int DS_ID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Chip_ID { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Plant { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Module { get; set; }
        
        [Required]
        [StringLength(20)]
        public string UserName { get; set; }
        
        [Required]
        [StringLength(10)]
        public string MachineID { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Style { get; set; }
        
        public int DailySum { get; set; }
        
        [Required]
        public DateTime Date { get; set; }

        public string Operation { get; set; }

        public string Shift { get; set; }
    }
}
