using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling IoT output data operations and analytics
    /// </summary>
    public class iotout : Controller
    {
        private readonly AppDbContext37 _dbContext;
        private readonly AppDbContext38 _dbContext1;
        private readonly AppDbContext39 _dbContext2;
        private readonly AppDbContext47 _dbContext3;
        private readonly ILogger<iotout> _logger;

        /// <summary>
        /// Constructor that initializes database contexts and logger
        /// </summary>
        /// <param name="dbContext">Database context for IoT output data</param>
        /// <param name="dbContext1">Database context for tack time data</param>
        /// <param name="dbContext2">Database context for user SMV data</param>
        /// <param name="dbContext3">Database context for IoT test data</param>
        /// <param name="logger">Logger instance for tracking operations</param>
        public iotout(AppDbContext37 dbContext, AppDbContext38 dbContext1, AppDbContext39 dbContext2, AppDbContext47 dbContext3, ILogger<iotout> logger)
        {   
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
            _dbContext3 = dbContext3;
            _logger = logger;
        }

        /// <summary>
        /// Displays the IoT output interface
        /// </summary>
        /// <returns>Returns the IoT output view</returns>
        public IActionResult Index()
        {
            return View("~/Views/iotout.cshtml");
        }

        /// <summary>
        /// Retrieves and processes IoT output data including efficiency and utilization metrics
        /// </summary>
        /// <returns>Returns JSON containing processed IoT output data</returns>
        [HttpGet]
        public async Task<IActionResult> GetIotData()
        {
            var modules = new[] { "FOF1", "FOF2", "FOF3", "L1", "L2" };

            // Get tack times for all modules
            var tacks = await _dbContext1.KreedaIot_TackTime
                                         .Where(t => modules.Contains(t.Module))
                                         .ToListAsync();

            if (tacks == null || tacks.Count < modules.Length)
            {
                return NotFound("Tack times not found.");
            }

            var tackDict = tacks.ToDictionary(t => t.Module, t => t.Tack);

            // Calculate targets for all modules
            var targets = tackDict.ToDictionary(kvp => kvp.Key, kvp => 3600 / kvp.Value);

            // Get IoT output data for MEC plant
            var iotData = await _dbContext.IotOUT
                  .Where(i => i.Plant == "MEC")
                                           .Select(i => new
                                           {
                                               i.ChipID,
                                               i.H1,
                                               i.H2,
                                               i.H3,
                                               i.H4,
                                               i.H5,
                                               i.H6,
                                               i.H7,
                                               i.H8,
                                               i.H9,
                                               i.UserName,
                                               i.Module,
                                               i.Operation,
                                               i.MAC,
                                               i.style,
                                               i.MachineID,
                                               i.Plant,
                                               Target = targets.ContainsKey(i.Module) ? targets[i.Module] : 0
                                           })
                                           .ToListAsync();

            // Get user SMV data
            var userSmvs = await _dbContext2.KreedIot_UserSMV
                                            .Select(u => new
                                            {
                                                u.UserName,
                                                u.style,
                                                u.Operation,
                                                SumHandSew = u.Hand + u.sew
                                            })
                                            .ToDictionaryAsync(u => (u.UserName, u.style, u.Operation), u => u.SumHandSew);

            var currentHour = GetCurrentHour();

            // Process and calculate metrics for each IoT data point
            var result = iotData.Select(i =>
            {
                var sumOfH = i.H1 + i.H2 + i.H3 + i.H4 + i.H5 + i.H6 + i.H7 + i.H8 + i.H9;
                var key = (i.UserName, i.style, i.Operation);
                var sumOfHandSew = userSmvs.ContainsKey(key) ? userSmvs[key] : 0;
                var summ = sumOfHandSew * sumOfH;
                var workingSeconds = CalculateWorkingSeconds();
                var realTimeTarget = i.Target * currentHour;
                var efficiency = realTimeTarget > 0 ? (sumOfH / (double)realTimeTarget) * 100 : 0;
                var utilization = workingSeconds > 0 ? (int)Math.Round((summ / (double)workingSeconds) * 100) : 0;

                return new
                {
                    i.ChipID,
                    i.H1,
                    i.H2,
                    i.H3,
                    i.H4,
                    i.H5,
                    i.H6,
                    i.H7,
                    i.H8,
                    i.H9,
                    i.UserName,
                    i.Module,
                    i.Operation,
                    i.Target,
                    sum = sumOfH,
                    i.MAC,
                    i.style,
                    i.MachineID,
                    i.Plant,
                    Taregt = realTimeTarget,
                    Utilization = utilization.ToString("F0") + "%"
                };
            }).ToList();

            return Json(result);
        }

        /// <summary>
        /// Calculates the current working hour based on time of day
        /// </summary>
        /// <returns>Returns the current working hour (1-9)</returns>
        private int GetCurrentHour()
        {
            var now = DateTime.Now;
            if (now.TimeOfDay >= TimeSpan.FromHours(7.5) && now.TimeOfDay < TimeSpan.FromHours(8.83)) return 1;
            if (now.TimeOfDay >= TimeSpan.FromHours(8.83) && now.TimeOfDay < TimeSpan.FromHours(9.83)) return 2;
            if (now.TimeOfDay >= TimeSpan.FromHours(9.83) && now.TimeOfDay < TimeSpan.FromHours(10.83)) return 3;
            if (now.TimeOfDay >= TimeSpan.FromHours(10.83) && now.TimeOfDay < TimeSpan.FromHours(11.83)) return 4;
            if (now.TimeOfDay >= TimeSpan.FromHours(11.83) && now.TimeOfDay < TimeSpan.FromHours(13.33)) return 5;
            if (now.TimeOfDay >= TimeSpan.FromHours(13.33) && now.TimeOfDay < TimeSpan.FromHours(14.33)) return 6;
            if (now.TimeOfDay >= TimeSpan.FromHours(14.33) && now.TimeOfDay < TimeSpan.FromHours(15.33)) return 7;
            if (now.TimeOfDay >= TimeSpan.FromHours(15.33) && now.TimeOfDay < TimeSpan.FromHours(16.5)) return 8;
            if (now.TimeOfDay >= TimeSpan.FromHours(16.5) && now.TimeOfDay < TimeSpan.FromHours(17.5)) return 9;
            return 9; // Outside working hours
        }

        /// <summary>
        /// Calculates the total working seconds for the current day
        /// </summary>
        /// <returns>Returns the total working seconds excluding breaks</returns>
        private int CalculateWorkingSeconds()
        {
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, sriLankaTimeZone);

            var startTime = new DateTime(now.Year, now.Month, now.Day, 7, 15, 0);
            var endTime = new DateTime(now.Year, now.Month, now.Day, 17, 25, 0);

            if (now > endTime)
            {
                now = endTime;
            }

            var elapsedSeconds = (int)(now - startTime).TotalSeconds;

            // Subtract break times
            if (now.TimeOfDay > new TimeSpan(8, 25, 0) && now.TimeOfDay <= new TimeSpan(12, 45, 0))
            {
                elapsedSeconds -= 1200; // Morning break
            }
            else if (now.TimeOfDay > new TimeSpan(12, 45, 0) && now.TimeOfDay <= new TimeSpan(15, 30, 0))
            {
                elapsedSeconds -= 3000; // Lunch break
            }
            else if (now.TimeOfDay > new TimeSpan(15, 30, 0) && now.TimeOfDay <= new TimeSpan(17, 25, 0))
            {
                elapsedSeconds -= 3600; // Evening break
            }

            return elapsedSeconds;
        }

        /// <summary>
        /// Deletes IoT test data based on specified criteria
        /// </summary>
        /// <param name="deleteData">The criteria for deleting IoT test data</param>
        /// <returns>Returns success status of the deletion operation</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteIotData([FromBody] DeleteIotDataRequest deleteData)
        {
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, sriLankaTimeZone).Date;

            try
            {
                _logger.LogInformation($"Delete request received for ChipId: {deleteData.chipid}, Style: {deleteData.style}, MAC: {deleteData.mac}, UserName: {deleteData.username}, Module: {deleteData.module}, Operation: {deleteData.operation}");

                var rowsToDeleteQuery = _dbContext3.KreedaIotTestNew
                                                   .Where(r => r.ChipID == deleteData.chipid &&
                                                               r.style == deleteData.style &&
                                                               r.MAC == deleteData.mac &&
                                                               r.UserName == deleteData.username &&
                                                               r.Module == deleteData.module &&
                                                               r.Operation == deleteData.operation &&
                                                               r.Date == now);

                _logger.LogInformation($"Generated SQL query: {rowsToDeleteQuery.ToQueryString()}");

                var rowsToDelete = await rowsToDeleteQuery.ToListAsync();

                if (!rowsToDelete.Any())
                {
                    _logger.LogWarning($"No rows found for the criteria: ChipId={deleteData.chipid}, Style={deleteData.style}, MAC={deleteData.mac}, UserName={deleteData.username}, Module={deleteData.module}, Operation={deleteData.operation}, Date={now}");
                    return Json(new { success = false, message = "No rows found to delete." });
                }

                foreach (var row in rowsToDelete)
                {
                    _logger.LogInformation($"Row found: ID={row.ID}, ChipID={row.ChipID}, Style={row.style}, MAC={row.MAC}, UserName={row.UserName}, Module={row.Module}, Operation={row.Operation}, Date={row.Date}");

                    try
                    {
                        _logger.LogInformation($"ChipID: {row.ChipID}");
                        _logger.LogInformation($"Style: {row.style}");
                        _logger.LogInformation($"MAC: {row.MAC}");
                        _logger.LogInformation($"UserName: {row.UserName}");
                        _logger.LogInformation($"Module: {row.Module}");
                        _logger.LogInformation($"Operation: {row.Operation}");
                        _logger.LogInformation($"Date: {row.Date}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accessing row data: {Message}", ex.Message);
                    }
                }

                _dbContext3.KreedaIotTestNew.RemoveRange(rowsToDelete);
                await _dbContext3.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException invalidCastEx)
                {
                    _logger.LogError(invalidCastEx, "InvalidCastException occurred: {Message}", invalidCastEx.Message);
                }
                else
                {
                    _logger.LogError(ex, "Error occurred while deleting data: {Message}", ex.Message);
                }
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Request model for deleting IoT test data
        /// </summary>
        public class DeleteIotDataRequest
        {
            /// <summary>
            /// The chip ID to delete
            /// </summary>
            public string chipid { get; set; }

            /// <summary>
            /// The style to delete
            /// </summary>
            public string style { get; set; }

            /// <summary>
            /// The MAC address to delete
            /// </summary>
            public string mac { get; set; }

            /// <summary>
            /// The username to delete
            /// </summary>
            public string username { get; set; }

            /// <summary>
            /// The module to delete
            /// </summary>
            public string module { get; set; }

            /// <summary>
            /// The operation to delete
            /// </summary>
            public string operation { get; set; }
        }

        /// <summary>
        /// Database context for managing IoT output data
        /// </summary>
        public class AppDbContext37 : DbContext
        {
            public DbSet<IotOut> IotOUT { get; set; }

            public AppDbContext37(DbContextOptions<AppDbContext37> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<IotOut>().HasNoKey();
            }
        }

        /// <summary>
        /// Database context for managing tack time data
        /// </summary>
        public class AppDbContext38 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext38(DbContextOptions<AppDbContext38> options) : base(options)
            {
            }
        }

        /// <summary>
        /// Database context for managing user SMV data
        /// </summary>
        public class AppDbContext39 : DbContext
        {
            public DbSet<UserSMV> KreedIot_UserSMV { get; set; }

            public AppDbContext39(DbContextOptions<AppDbContext39> options) : base(options)
            {
            }
        }

        /// <summary>
        /// Database context for managing IoT test data
        /// </summary>
        public class AppDbContext47 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext47(DbContextOptions<AppDbContext47> options) : base(options)
            {
            }
        }
    }
}
