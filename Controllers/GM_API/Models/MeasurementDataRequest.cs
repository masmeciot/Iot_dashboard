namespace Iot_dashboard.Controllers.GM_API.Models
{
    public class MeasurementDataRequest
    {
        public string Style { get; set; }
        public string Size { get; set; }
        public string DateFilter { get; set; } // "Yes" or "No"
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}