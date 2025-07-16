namespace Iot_dashboard.Controllers.GM_API.Models
{
    public class MeasurementInfo
    {
        public string Measurement { get; set; }
        public string Type { get; set; }
        public int Reference { get; set; }
        public int Tolerance { get; set; }
    }

    public class SizeMeasurements
    {
        public string Size { get; set; }
        public List<MeasurementInfo> Measurements { get; set; }
    }
}