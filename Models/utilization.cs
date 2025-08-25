namespace Iot_dashboard.Models
{
    public class Utilization
    {
        public int ID { get; set; }
        public string? IP { get; set; }
        public string? ChipID { get; set; }
        public string? UID { get; set; }
        public string? UserName { get; set; }
        public string? GatewayIp { get; set; }
        public string? MAC { get; set; }
        public int? TotalRuntime { get; set; }
        public int? StitchCount { get; set; }
        public int? RPM { get; set; }
        public int? MotorRunTime { get; set; }
        public int? MotorStopTime { get; set; }
        public int? DeltaTime { get; set; }
        public DateTime? Date { get; set; }
        public TimeSpan? Time { get; set; }
        public int? Hour { get; set; }
        public string? Shift { get; set; }
        public string? Module { get; set; }
        public string? Plant { get; set; }
        public string? Operation { get; set; }
        public string? MachineID { get; set; }
        public string? Style { get; set; }
        public DateTime? DateTime { get; set; }
        public int? EpochT { get; set; }
        public string? StationID { get; set; }
        public int? HSMV { get; set; }
        public int? SSMV { get; set; }
       
    }
}