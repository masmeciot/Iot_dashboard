using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Iot_dashboard.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Azure.Devices;
using Azure;
using Newtonsoft.Json;
using System.Text;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling IoT data insertion and deletion operations
    /// </summary>
    public class iotplusminus : Controller
    {
        private static string ConnectionString = "HostName=KRE-SEA-KRE-IOT-HUB.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=FYsdT36eyqnN6nhcKu/0X72ISo2DccPc7VIKvhh3Oj0=";
        private static ServiceClient serviceClient;

        private readonly AppDbContext48 _dbContext;
        private readonly AppDbContext49 _dbContext1;
        private readonly AppDbContext50 _dbContext2;
        private readonly ILogger<iotplusminus> _logger;

        /// <summary>
        /// Database context for managing IoT test data
        /// </summary>
        public class AppDbContext48 : DbContext
        {
            public DbSet<iotplusmodel> KreedaIotTestNew { get; set; }

            public AppDbContext48(DbContextOptions<AppDbContext48> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
               
            }
        }

        /// <summary>
        /// Database context for managing IoT machine details
        /// </summary>
        public class AppDbContext50 : DbContext
        {
            public DbSet<device> KreedaIoTMachineDetails { get; set; }

            public AppDbContext50(DbContextOptions<AppDbContext50> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {

            }
        }

        /// <summary>
        /// Database context for managing IoT output data
        /// </summary>
        public class AppDbContext49 : DbContext
        {
            public DbSet<IotOut> IotOUT { get; set; }

            public AppDbContext49(DbContextOptions<AppDbContext49> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<IotOut>().HasNoKey();
            }
        }

        /// <summary>
        /// Constructor that initializes database contexts and logger
        /// </summary>
        /// <param name="dbContext">Context for IoT test data</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <param name="dbContext1">Context for IoT output data</param>
        /// <param name="dbContext2">Context for IoT machine details</param>
        public iotplusminus(AppDbContext48 dbContext, ILogger<iotplusminus> logger, AppDbContext49 dbContext1, AppDbContext50 dbContext2)
        {
            _dbContext = dbContext;
            _logger = logger;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
        }

        /// <summary>
        /// Displays the IoT plus/minus interface
        /// </summary>
        /// <returns>Returns the IoT plus/minus view</returns>
        public IActionResult Index()
        {
            return View("~/Views/iotPlusMinus.cshtml");
        }

        /// <summary>
        /// Inserts new IoT data and sends response to the device
        /// </summary>
        /// <param name="data">The IoT data to insert</param>
        /// <returns>Returns success message if data is inserted successfully</returns>
        [HttpPost]
        public async Task<IActionResult> InsertData([FromBody] iotplusmodel data)
        {
            List<int> hData = new List<int>();
            serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString);
            try
            {
                if (ModelState.IsValid)
                {
                    TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
                    DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);

                    data.Date = sriLankanTime.Date;
                    data.RequestID = 0;

                    // Generate a random 6-digit number for EpochT
                    Random random = new Random();
                    int epochT = random.Next(100000, 999999);

                    // Use raw SQL to insert data without OUTPUT clause
                    var sql = @"
                INSERT INTO [KreedaIotTestNew] ([ChipID], [Date], [GatewayIp], [IP], [MAC], [MachineID], [Module], [Operation], [Plant], [EpochT], [Shift], [Time], [UID], [UserName], [style], [StationID])
                VALUES (@ChipID, @Date, @GatewayIp, @IP, @MAC, @MachineID, @Module, @Operation, @Plant, @EpochT, @Shift, @Time, @UID, @UserName, @style, 0);";

                    var parameters = new[]
                    {
                new SqlParameter("@ChipID", data.ChipID),
                new SqlParameter("@Date", data.Date),
                new SqlParameter("@GatewayIp", data.GatewayIp ?? (object)DBNull.Value),
                new SqlParameter("@IP", data.IP ?? (object)DBNull.Value),
                new SqlParameter("@MAC", data.MAC),
                new SqlParameter("@MachineID", data.MachineID),
                new SqlParameter("@Module", data.Module),
                new SqlParameter("@Operation", data.Operation),
                new SqlParameter("@Plant", data.Plant),
                new SqlParameter("@EpochT", epochT), // Add generated EpochT
                new SqlParameter("@Shift", data.Shift ?? (object)DBNull.Value),
                new SqlParameter("@Time", data.Time),
                new SqlParameter("@UID", data.UID),
                new SqlParameter("@UserName", data.UserName),
                new SqlParameter("@style", data.style)
            };

                    await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);

                    var deviceId = await _dbContext2.KreedaIoTMachineDetails
                         .Where(x => x.MAC == data.MAC)
                         .Select(x => x.KreedIotDeviceID)
                         .FirstOrDefaultAsync();

                    var macRecord = await _dbContext1.IotOUT
                             .Where(z => z.style == data.style && z.Operation == data.Operation)
                             .FirstOrDefaultAsync();

                    if (macRecord != null)
                    {
                        hData.Add(macRecord.H1);
                        hData.Add(macRecord.H2);
                        hData.Add(macRecord.H3);
                        hData.Add(macRecord.H4);
                        hData.Add(macRecord.H5);
                        hData.Add(macRecord.H6);
                        hData.Add(macRecord.H7);
                        hData.Add(macRecord.H8);
                        hData.Add(macRecord.H9);
                    }

                    var responsePayload = new
                    {
                        HData = hData
                    };
                    string jsonResponse = JsonConvert.SerializeObject(responsePayload);
                    Console.WriteLine("Response : " + jsonResponse);

                    var responseMessage = new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(jsonResponse));
                    await serviceClient.SendAsync(deviceId, responseMessage);

                    Console.WriteLine("Response sent successfully to: " + deviceId);

                    return Ok(new { message = "Data inserted successfully" });
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                   .Select(e => e.ErrorMessage)
                                                   .ToList();

                    _logger.LogWarning("Invalid data received: {Errors}", errors);
                    return BadRequest(new { message = "Invalid data", errors = errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while inserting data");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes IoT data based on specified criteria and sends response to the device
        /// </summary>
        /// <param name="request">The delete request containing criteria for deletion</param>
        /// <returns>Returns success message if data is deleted successfully</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteData([FromBody] DeleteDataRequest request)
        {
            List<int> hData = new List<int>();
            serviceClient = ServiceClient.CreateFromConnectionString(ConnectionString);

            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);

            try
            {
                var timeMapping = new Dictionary<int, (TimeSpan, TimeSpan)>
            {
                { 1, (new TimeSpan(7, 30, 0), new TimeSpan(8, 50, 0)) },
                { 2, (new TimeSpan(8, 50, 0), new TimeSpan(9, 50, 0)) },
                { 3, (new TimeSpan(9, 50, 0), new TimeSpan(10, 50, 0)) },
                { 4, (new TimeSpan(10, 50, 0), new TimeSpan(11, 50, 0)) },
                { 5, (new TimeSpan(11, 50, 0), new TimeSpan(13, 20, 0)) },
                { 6, (new TimeSpan(13, 20, 0), new TimeSpan(14, 20, 0)) },
                { 7, (new TimeSpan(14, 20, 0), new TimeSpan(15, 20, 0)) },
                { 8, (new TimeSpan(15, 20, 0), new TimeSpan(16, 30, 0)) },
                { 9, (new TimeSpan(16, 30, 0), new TimeSpan(17, 30, 0)) }
            };

                var (startTime, endTime) = timeMapping[request.ButtonId];

                _logger.LogInformation("Deleting data with criteria: ChipID={ChipID}, MAC={MAC}, UserName={UserName}, Operation={Operation}, style={style}, startTime={startTime}, endTime={endTime}, Date={date}",
                    request.ChipID, request.MAC, request.UserName, request.Operation, request.style, startTime, endTime, sriLankanTime.Date);

                var entities = await _dbContext.KreedaIotTestNew
                    .Where(e =>
                        e.Date == sriLankanTime.Date &&
                        e.ChipID == request.ChipID &&
                        e.MAC == request.MAC &&
                        e.UserName == request.UserName &&
                        e.Operation == request.Operation &&
                        e.style == request.style &&
                        e.Time >= startTime &&
                        e.Time < endTime)
                    .OrderByDescending(e => e.Time) 
                    .Take(request.ClickCount) 
                    .ToListAsync();

                if (entities != null && entities.Count > 0)
                {
                    _dbContext.KreedaIotTestNew.RemoveRange(entities);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Deleted {entities.Count} records successfully");

                var deviceId = await _dbContext2.KreedaIoTMachineDetails
                      .Where(x => x.MAC == request.MAC)
                       .Select(x => x.KreedIotDeviceID)
                        .FirstOrDefaultAsync();

                    var macRecord = await _dbContext1.IotOUT
                            .Where(z => z.style == request.style && z.Operation == request.Operation)
                            .FirstOrDefaultAsync();

                    if (macRecord != null)
                    {
                        hData.Add(macRecord.H1);
                        hData.Add(macRecord.H2);
                        hData.Add(macRecord.H3);
                        hData.Add(macRecord.H4);
                        hData.Add(macRecord.H5);
                        hData.Add(macRecord.H6);
                        hData.Add(macRecord.H7);
                        hData.Add(macRecord.H8);
                        hData.Add(macRecord.H9);
                    }

                    var responsePayload = new
                    {
                        HData = hData
                    };
                    string jsonResponse = JsonConvert.SerializeObject(responsePayload);
                    Console.WriteLine("Response : " + jsonResponse);

                    var responseMessage = new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(jsonResponse));
                    await serviceClient.SendAsync(deviceId, responseMessage);

                    Console.WriteLine("Response sent successfully to: " + deviceId);
                }

                return Ok(new { message = "Data deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting data");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for deleting IoT data
    /// </summary>
    public class DeleteDataRequest
    {
        /// <summary>
        /// The chip ID of the device
        /// </summary>
        public string ChipID { get; set; }

        /// <summary>
        /// The MAC address of the device
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// The username associated with the operation
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The operation being performed
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// The style being processed
        /// </summary>
        public string style { get; set; }

        /// <summary>
        /// The ID of the button that triggered the deletion
        /// </summary>
        public int ButtonId { get; set; }

        /// <summary>
        /// The number of records to delete
        /// </summary>
        public int ClickCount { get; set; }
    }
}
