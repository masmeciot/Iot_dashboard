using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Iot_dashboard.Controllers.Iot.Iotconfig;
using Iot_dashboard.Models;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Synergy
{
    /// <summary>
    /// Controller responsible for handling manual output operations for Synergy devices
    /// </summary>
    public class manualOutput : Controller
    {
        /// <summary>
        /// Database context for managing IoT machine details
        /// </summary>
        public class AppDbContext81 : DbContext
        {
            public DbSet<device> KreedaIoTMachineDetails { get; set; }

            public AppDbContext81(DbContextOptions<AppDbContext81> options) : base(options)
            {
            }
        }

        private static string connectionString = "HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=";
        private readonly AppDbContext81 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT machine details</param>
        public manualOutput(AppDbContext81 dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Sends a message to a specific IoT device through Azure IoT Hub
        /// </summary>
        /// <param name="deviceId">The ID of the target device</param>
        /// <param name="messageContent">The content of the message to send</param>
        private static async Task SendCloudToDeviceMessageAsync(string deviceId, string messageContent)
        {
            try
            {
                using var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                var message = new Message(System.Text.Encoding.UTF8.GetBytes(messageContent));
                await serviceClient.SendAsync(deviceId, message);
                Console.WriteLine($"Message '{messageContent}' sent to device '{deviceId}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to IoT Hub: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays the manual output interface
        /// </summary>
        /// <returns>Returns the manual output view</returns>
        public IActionResult Index()
        {
            return View("~/Views/synergy/ManualOutput.cshtml");
        }

        /// <summary>
        /// Handles the manual output command submission
        /// </summary>
        /// <param name="Module">The module identifier</param>
        /// <param name="station">The station identifier</param>
        /// <param name="Hour">The hour value for the command</param>
        /// <param name="total">The total value for the command</param>
        /// <returns>Returns a JSON response indicating success or failure of the command</returns>
        [HttpPost]
        public async Task<IActionResult> Index1(string Module, string station, string Hour, string total)
        {
            try
            {
                // Find the device based on Module and StationID
                var device = await _dbContext.KreedaIoTMachineDetails
                    .FirstOrDefaultAsync(d => d.Module == Module && d.StationID == station);

                if (device == null || string.IsNullOrEmpty(device.KreedIotDeviceID))
                {
                    // Return error response if device not found
                    return Json(new { success = false, message = "Device not found for the given Module and Station" });
                }

                // Format the message as "V+Hour,+Total"
                string messageContent = $"V{Hour},{total}";

                // Send message to Azure IoT Hub using KreedIotDeviceID
                await SendCloudToDeviceMessageAsync(device.KreedIotDeviceID, messageContent);

                // Return success response
                return Json(new { success = true, message = "Command sent successfully" });
            }
            catch (Exception ex)
            {
                // Return error response
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}