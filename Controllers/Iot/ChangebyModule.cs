using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using Microsoft.Azure.Devices;
using static Iot_dashboard.Controllers.Iot.Iotconfig;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling module-level changes and device synchronization
    /// </summary>
    public class ChangebyModule : Controller
    {
        /// <summary>
        /// Database context for managing IoT machine details
        /// </summary>
        public class AppDbContext66 : DbContext
        {
            public DbSet<device> KreedaIoTMachineDetails { get; set; }

            public AppDbContext66(DbContextOptions<AppDbContext66> options) : base(options)
            {
            }
        }

        private static string connectionString = "HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=";

        private readonly AppDbContext66 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT machine details</param>
        public ChangebyModule(AppDbContext66 dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Updates the style for all devices in a specific plant and module
        /// </summary>
        /// <param name="request">The update request containing plant, module, and new style</param>
        /// <returns>Returns success message if style is updated successfully</returns>
        [HttpPost]
        public IActionResult UpdateStyle([FromBody] UpdateStyleRequest request)
        {
            try
            {
                var devicesToUpdate = _dbContext.KreedaIoTMachineDetails
                    .Where(d => d.PlantName == request.Plant && d.Module == request.Module)
                    .ToList();

                if (!devicesToUpdate.Any())
                {
                    // Log if no matching records are found
                    return NotFound(new { message = "No devices found for the specified plant and module" });
                }

                // Update the style for each matching device
                foreach (var device in devicesToUpdate)
                {
                    device.style = request.Style;
                }

                _dbContext.SaveChanges(); // Save changes to persist the update
                return Ok(new { message = "Style updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine("Error updating style: " + ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating the style" });
            }
        }

        /// <summary>
        /// Request model for updating device styles
        /// </summary>
        public class UpdateStyleRequest
        {
            /// <summary>
            /// The plant name to update
            /// </summary>
            public string Plant { get; set; }

            /// <summary>
            /// The module to update
            /// </summary>
            public string Module { get; set; }

            /// <summary>
            /// The new style to set
            /// </summary>
            public string Style { get; set; }
        }

        /// <summary>
        /// Updates the operation for devices matching a specific chip ID
        /// </summary>
        /// <param name="request">The update request containing chip ID and new operation</param>
        /// <returns>Returns success message if operation is updated successfully</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateOperation([FromBody] UpdateOperationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ChipID) || string.IsNullOrWhiteSpace(request.Operation))
            {
                return BadRequest(new { message = "Device number and operation cannot be empty." });
            }

            try
            {
                var devicesToUpdate = await _dbContext.KreedaIoTMachineDetails
                    .Where(d => EF.Functions.Like(d.ChipID, $"%{request.ChipID}%"))
                    .ToListAsync();

                if (!devicesToUpdate.Any())
                {
                    return NotFound(new { message = "No matching devices found." });
                }

                foreach (var device in devicesToUpdate)
                {
                    device.Operation = request.Operation;
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Operation updated successfully for matching devices." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Request model for updating device operations
        /// </summary>
        public class UpdateOperationRequest
        {
            /// <summary>
            /// The chip ID to match devices
            /// </summary>
            public string ChipID { get; set; }

            /// <summary>
            /// The new operation to set
            /// </summary>
            public string Operation { get; set; }
        }

        /// <summary>
        /// Synchronizes devices in a specific plant and module by sending SYNC messages
        /// </summary>
        /// <param name="request">The sync request containing plant and module</param>
        /// <returns>Returns success count and failure count of sync operations</returns>
        [HttpPost]
        public async Task<IActionResult> SyncDevices([FromBody] SyncRequest request)
        {
            try
            {
                Console.WriteLine($"SyncDevices called with Plant: {request.Plant}, Module: {request.Module}");

                // Query devices and exclude null or empty DeviceId values
                Console.WriteLine("Querying database for matching devices...");
                var deviceIds = await _dbContext.KreedaIoTMachineDetails
                    .Where(d => d.PlantName == request.Plant && d.Module == request.Module && !string.IsNullOrEmpty(d.KreedIotDeviceID))
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
                            var message = new Message(System.Text.Encoding.UTF8.GetBytes("SYNC"));
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
        /// Request model for device synchronization
        /// </summary>
        public class SyncRequest
        {
            /// <summary>
            /// The plant to sync devices in
            /// </summary>
            public string Plant { get; set; }

            /// <summary>
            /// The module to sync devices in
            /// </summary>
            public string Module { get; set; }
        }

        /// <summary>
        /// Displays the change by module interface
        /// </summary>
        /// <returns>Returns the change by module view</returns>
        public IActionResult Index()
        {
            return View("~/Views/ChangeByModule.cshtml");
        }
    }
}
