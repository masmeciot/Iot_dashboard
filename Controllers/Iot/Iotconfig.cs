using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using Iot_dashboard.Models.AMR;
using Microsoft.Azure.Devices;

namespace Iot_dashboard.Controllers.Iot
{
    public class Iotconfig : Controller
    {
        public class AppDbContext34 : DbContext
        {
            public DbSet<device> KreedaIoTMachineDetails { get; set; }

            public AppDbContext34(DbContextOptions<AppDbContext34> options) : base(options)
            {
            }
        }

        public class AppDbContext59 : DbContext
        {
            public DbSet<UserSMV> KreedIot_UserSMV { get; set; }

            public AppDbContext59(DbContextOptions<AppDbContext59> options) : base(options)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.EnableSensitiveDataLogging();

            }
        }

        private static string connectionString = "HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=";

        private readonly AppDbContext34 _dbContext;
        private readonly AppDbContext59 _dbContext1;

        public Iotconfig(AppDbContext34 dbContext, AppDbContext59 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;


        }
        [HttpGet]
        public IActionResult SearchBy(string snm, string mac)
        {
            try
            {
                var slotData = _dbContext.KreedaIoTMachineDetails
                    .FirstOrDefault(p =>
                        (string.IsNullOrEmpty(snm) || p.ChipID == snm) &&
                        (string.IsNullOrEmpty(mac) || p.MAC == mac));

                if (slotData != null)
                {
                    return Json(new
                    {
                        operation = slotData.Operation,
                        mac = slotData.MAC,
                        machinesn = slotData.ChipID,
                        plant = slotData.PlantName,
                        user = slotData.UserLogged,
                        machine = slotData.MachineType,
                        shift = slotData.NoOfShifts,
                        zone = slotData.NoZones,
                        auto = slotData.AutoCode,
                        deviceid = slotData.KreedIotDeviceID,
                        con = slotData.ConnectionString,
                        module = slotData.Module,
                        style = slotData.style,
                        mode = slotData.Mode,
                        staionid = slotData.StationID
                    });
                }

                return Json(new { });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost]
        public IActionResult AddNewData([FromBody] device newData)
        {
            try
            {
                // Check if a record with the same ChipID already exists
                var existingData = _dbContext.KreedaIoTMachineDetails
                                             .FirstOrDefault(p => p.ChipID == newData.ChipID);

                if (existingData != null)
                {
                    // Delete the existing record
                    _dbContext.KreedaIoTMachineDetails.Remove(existingData);
                    _dbContext.SaveChanges();
                }

                // Add the new data
                _dbContext.KreedaIoTMachineDetails.Add(newData);
                _dbContext.SaveChanges();

                return Ok("Data replaced successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpPost]
        public async Task<IActionResult> UpdateData([FromBody] device UpData)
        {
            if (UpData == null)
            {
                return BadRequest("Update data is null");
            }

            try
            {
                // Find the existing data by ChipID
                var existingData = _dbContext.KreedaIoTMachineDetails.FirstOrDefault(p => p.ChipID == UpData.ChipID);

                if (existingData == null)
                {
                    return NotFound("Data not found for the given ChipID");
                }

                // Update fields
                existingData.PlantName = UpData.PlantName ?? existingData.PlantName;
                existingData.Module = UpData.Module ?? existingData.Module;
                existingData.MAC = UpData.MAC ?? existingData.MAC;
                existingData.Operation = UpData.Operation ?? existingData.Operation;
                existingData.UserLogged = UpData.UserLogged ?? existingData.UserLogged;
                existingData.MachineType = UpData.MachineType ?? existingData.MachineType;
                existingData.NoOfShifts = UpData.NoOfShifts;
                existingData.NoZones = UpData.NoZones;
                existingData.StationID = UpData.StationID;
                existingData.KreedIotDeviceID = UpData.KreedIotDeviceID ?? existingData.KreedIotDeviceID;
                existingData.ConnectionString = UpData.ConnectionString ?? existingData.ConnectionString;
                existingData.style = UpData.style ?? existingData.style;
                existingData.Mode = UpData.Mode;


            

                // Save changes to the database
                _dbContext.SaveChanges();

                // Send a message to the IoT Hub
                if (!string.IsNullOrWhiteSpace(UpData.KreedIotDeviceID))
                {
                    await SendCloudToDeviceMessageAsync(UpData.KreedIotDeviceID, "SYNC");
                }

                return Ok("Data updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred while updating data: " + ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private static async Task SendCloudToDeviceMessageAsync(string deviceId, string messageContent)
        {
            try
            {
                // Create a ServiceClient instance
                using var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

                // Create and send a cloud-to-device message
                var message = new Message(System.Text.Encoding.UTF8.GetBytes(messageContent));
                await serviceClient.SendAsync(deviceId, message);

                Console.WriteLine($"Message '{messageContent}' sent to device '{deviceId}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to IoT Hub: {ex.Message}");
            }
        }





        [HttpPost]
        public IActionResult DeleteData([FromBody] string snm)
        {
            try
            {
                var dataToDelete = _dbContext.KreedaIoTMachineDetails.FirstOrDefault(p => p.ChipID == snm);

                if (dataToDelete != null)
                {
                    _dbContext.KreedaIoTMachineDetails.Remove(dataToDelete);
                    _dbContext.SaveChanges();
                    return Ok("Data deleted successfully");
                }
                else
                {
                    return NotFound("Data not found for the given MachineSn");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet]
        public IActionResult GetData()
        {
            try
            {
                var data = _dbContext.KreedaIoTMachineDetails
                    .Select(d => new
                    {
                        Operation = d.Operation ?? string.Empty,
                        MAC = d.MAC ?? string.Empty,
                        MachineSn = d.ChipID ?? string.Empty,
                        PlantName = d.PlantName ?? string.Empty,
                        UserLogged = d.UserLogged ?? string.Empty,
                        MachineType = d.MachineType ?? string.Empty,
                        NoOfShifts = d.NoOfShifts.HasValue ? d.NoOfShifts.Value : 0,
                        NoZones = d.NoZones.HasValue ? d.NoZones.Value : 0,
                        StationID = d.StationID,
                        KreedIotDeviceID = d.KreedIotDeviceID ?? string.Empty,
                        ConnectionString = d.ConnectionString ?? string.Empty,
                        Module = d.Module ?? string.Empty,
                         style = d.style ?? string.Empty,
                        Mode = d.Mode ?? string.Empty
                    })
                    .ToList();

                return Json(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet]
        public IActionResult GetDistinctOperations()
        {
            var distinctOperations = _dbContext1.KreedIot_UserSMV
                .Select(s => s.Operation)
                .Distinct()
                .ToList();

            return Ok(distinctOperations);
        }

        [HttpGet]
        public IActionResult GetDistinctStyles()
        {
            var distinctStyles = _dbContext1.KreedIot_UserSMV
                .Select(s => s.style)
                .Distinct()
                .ToList();

            return Ok(distinctStyles);
        }






        public IActionResult Index()
        {
            return View("~/Views/iotdeviceconfigdata.cshtml");
        }
    }
}
