using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iot_dashboard.Models
{

    public class device
    {
        public int ID { get; set; }
        public string StationID { get; set; }
        public string Operation { get; set; }
        public string MAC { get; set; }
        public string ChipID { get; set; }
        public string PlantName { get; set; }
        public string UserLogged { get; set; }
        public string MachineType { get; set; }
        public int? NoOfShifts { get; set; }
        public int? NoZones { get; set; }
        public string AutoCode { get; set; }
        public string KreedIotDeviceID { get; set; }
        public string ConnectionString { get; set; }
        public string Module { get; set; }
        public string style { get; set; }
        public string Mode { get; set; }

    }
}
