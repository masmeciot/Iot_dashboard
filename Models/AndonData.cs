using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{
    [Table("KreedaIot_Andon_raised")]
    public class KreedaIot_Andon_raised
    {
        public int ID { get; set; }
        public string machine_id { get; set; }
        public string user_raised_by { get; set; }
        public string module { get; set; }
        public DateTime andon_start_time { get; set; }
        public string andon_category { get; set; }
        public string andon_issue { get; set; }
        public DateTime andon_resolved_time { get; set; }
        public string resolved_by { get; set; }
        public DateTime date_raised { get; set; }
        public string status { get; set; }

    }
}
