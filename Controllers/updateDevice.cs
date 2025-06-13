using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers
{
    /// <summary>
    /// Controller responsible for managing IoT device updates and module synchronization
    /// </summary>
    public class UpdateDeviceController : Controller
    {
        /// <summary>
        /// Database context for managing IoT machine details
        /// </summary>
        public class AppDbContext72 : DbContext
        {
            public DbSet<device> KreedaIoTMachineDetails { get; set; }

            public AppDbContext72(DbContextOptions<AppDbContext72> options) : base(options)
            {
            }
        }
        private readonly AppDbContext72 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT machine details</param>
        public UpdateDeviceController(AppDbContext72 dbContext)
        {
            _dbContext = dbContext;
        }

        private static string connectionString = "HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=";

        /// <summary>
        /// Sends an update command to a specific IoT device
        /// </summary>
        /// <param name="deviceId">The ID of the device to update</param>
        /// <returns>Returns a JSON response indicating success or failure of the update command</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateDevice(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return Json(new { success = false, message = "Device ID is required." });
            }

            try
            {
                // Create a ServiceClient instance
                using var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

                // Create and send a cloud-to-device message
                var message = new Message(System.Text.Encoding.UTF8.GetBytes("UPDATE"));
                await serviceClient.SendAsync(deviceId, message);

                return Json(new { success = true, message = "Update message sent successfully!" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sends update commands to multiple devices within a specific module
        /// </summary>
        /// <param name="request">The sync request containing the module information</param>
        /// <returns>Returns a JSON response with the results of the batch update operation</returns>
        [HttpPost]
        public async Task<IActionResult> updatemodule([FromBody] SyncRequest request)
        {
            try
            {
                Console.WriteLine($"SyncDevices called with  Module: {request.module}");

                // Query devices and exclude null or empty DeviceId values
                Console.WriteLine("Querying database for matching devices...");
                var deviceIds = await _dbContext.KreedaIoTMachineDetails
                    .Where(d =>  d.Module == request.module && !string.IsNullOrEmpty(d.KreedIotDeviceID))
                    .Select(d => d.KreedIotDeviceID)
                    .ToListAsync();

                if (deviceIds.Count == 0)
                {
                    Console.WriteLine("No valid devices found for the provided plant and module.");
                    return BadRequest(new { message = "No devices found for the selected module and plant." });
                }

                Console.WriteLine($"Query successful. Found {deviceIds.Count} valid devices.");
                Console.WriteLine("Valid Device IDs:");
                foreach (var deviceId in deviceIds)
                {
                    Console.WriteLine(deviceId);
                }

                // Initialize Azure IoT Hub client
                var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
                Console.WriteLine("Initialized Azure IoT Hub client. Starting batch message sending...");

                int successCount = 0, failureCount = 0;
                int batchSize = 10; // Define your batch size here
                var batches = deviceIds.Select((deviceId, index) => new { deviceId, index })
                                       .GroupBy(x => x.index / batchSize, x => x.deviceId);

                foreach (var batch in batches)
                {
                    var tasks = batch.Select(async deviceId =>
                    {
                        try
                        {
                            var message = new Message(System.Text.Encoding.UTF8.GetBytes("UPDATE"));
                            await serviceClient.SendAsync(deviceId, message);
                            Console.WriteLine($"SYNC message sent to Device ID: {deviceId}");
                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send SYNC message to Device ID: {deviceId}. Error: {ex.Message}");
                            Interlocked.Increment(ref failureCount);
                        }
                    });

                    await Task.WhenAll(tasks); // Process all tasks in the batch concurrently
                }

                Console.WriteLine($"Batch message sending completed. Success: {successCount}, Failed: {failureCount}");
                return Ok(new { successCount, failureCount, message = "Batch sync completed with partial or full success." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Request model for module synchronization
        /// </summary>
        public class SyncRequest
        {
            /// <summary>
            /// The module identifier to sync
            /// </summary>
            public string module { get; set; }
        }
    }
}



