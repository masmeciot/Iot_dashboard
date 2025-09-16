using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Iot_dashboard.Controllers.Synergy
{
    public class synout : Controller
    {
        private readonly AppDbContext52 _dbContext;
        private readonly AppDbContext53 _dbContext1;
        private readonly AppDbContext54 _dbContext2;
        private readonly AppDbContext55 _dbContext3;
        private readonly AppDbContext82 _dbContext4;

        private readonly ILogger<synout> _logger;
        public synout(AppDbContext52 dbContext, AppDbContext53 dbContext1, AppDbContext54 dbContext2, AppDbContext55 dbContext3, AppDbContext82 dbContext4, ILogger<synout> logger)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
            _dbContext3 = dbContext3;
            _dbContext4 = dbContext4;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Views/synout.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetIotData(DateTime? date, string PlantName)
        {

            var iotData = await _dbContext.IoTHOut
                                           .Select(i => new
                                           {
                                               i.RecordDate,
                                               i.ChipID,
                                               i.H0,
                                               i.H1,
                                               i.H2,
                                               i.H3,
                                               i.H4,
                                               i.H5,
                                               i.H6,
                                               i.H7,
                                               i.H8,
                                               i.H9,
                                               i.H10,
                                               i.H11,
                                               i.H12,
                                               i.H13,
                                               i.H14,
                                               i.H15,
                                               i.H16,
                                               i.H17,
                                               i.H18,
                                               i.H19,
                                               i.H20,
                                               i.H21,
                                               i.H22,
                                               i.H23,
                                               i.UserName,
                                               i.Module,
                                               i.Operation,
                                               i.style,
                                               i.MachineID,
                                               i.Plant,
                                               i.MAC
                                           })
                                           .Where(i => i.Plant == PlantName && i.RecordDate == date.Value.Date)
                                           .ToListAsync();

            // Preload all UserSMV rows for efficient lookup
            var userSmvList = await _dbContext2.KreedIot_UserSMV
                .Select(u => new { u.Operation, u.sew })
                .ToListAsync();

            var result = iotData.Select(i =>
            {
                var sumOfH = i.H0 + i.H1 + i.H2 + i.H3 + i.H4 + i.H5 + i.H6 + i.H7 + i.H8 + i.H9 + i.H10 + i.H11 + i.H12 + i.H13 + i.H14 + i.H15 + i.H16 + i.H17 + i.H18 + i.H19 + i.H20 + i.H21 + i.H22 + i.H23;

                // Find the first matching UserSMV row for the operation
                var smvRow = userSmvList.FirstOrDefault(u => u.Operation == i.Operation);
                var sewSmv = smvRow?.sew ?? 1;

                // Pseudocode:
                // - When calculating 'target', if the result is a decimal, round it up to the next integer.
                // - Use Math.Ceiling to achieve this.
                // - Cast the result to int if needed.

                double target = sewSmv > 0 ? Math.Ceiling((3300.0 / sewSmv) * 0.8) : 1;

                return new
                {
                    i.RecordDate,
                    i.ChipID,
                    i.H0,
                    i.H1,
                    i.H2,
                    i.H3,
                    i.H4,
                    i.H5,
                    i.H6,
                    i.H7,
                    i.H8,
                    i.H9,
                    i.H10,
                    i.H11,
                    i.H12,
                    i.H13,
                    i.H14,
                    i.H15,
                    i.H16,
                    i.H17,
                    i.H18,
                    i.H19,
                    i.H20,
                    i.H21,
                    i.H22,
                    i.H23,
                    i.UserName,
                    i.Module,
                    i.Operation,
                    Target = target,
                    sum = sumOfH,
                    i.MAC,
                    i.style,
                    i.MachineID,
                    i.Plant
                };
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDailyData(DateTime startDate, DateTime endDate, string plantName, bool showEfficiency = false)
        {
            try
            {
                // Get daily sum data for the date range
                var dailyData = await _dbContext4.KreedaIot_DailySum
                    .Where(d => d.Plant == plantName && d.Date >= startDate && d.Date <= endDate)
                    .ToListAsync();

                // Get SMV data for efficiency calculation
                var smvData = await _dbContext2.KreedIot_UserSMV
                    .Select(u => new { u.Operation, u.sew })
                    .ToListAsync();

                // Group by ChipID to create rows
                var groupedData = dailyData.GroupBy(d => new { d.Chip_ID, d.Plant, d.Module, d.UserName, d.MachineID, d.Operation, d.Shift })
                    .Select(g => new
                    {
                        ChipID = g.Key.Chip_ID,
                        Plant = g.Key.Plant,
                        Module = g.Key.Module,
                        UserName = g.Key.UserName,
                        MachineID = g.Key.MachineID,
                        DailyData = g.ToList(),
                        Operation = g.Key.Operation,
                        Shift = g.Key.Shift
                    })
                    .ToList();

                var result = new List<object>();

                foreach (var group in groupedData)
                {
                    // Calculate daily target using group's operation SMV and dynamic shift duration
                    var smvRowForGroup = smvData.FirstOrDefault(s => s.Operation == group.Operation);
                    var sewSmvForGroup = smvRowForGroup?.sew ?? 1;
                    var shiftDuration = CalculateShiftDuration(group.Shift);
                    var dailyTargetDouble = Math.Ceiling((3300.0 / (sewSmvForGroup > 0 ? sewSmvForGroup : 1)) * 0.8) * shiftDuration;
                    var dailyTarget = (int)dailyTargetDouble;

                    var rowData = new Dictionary<string, object>
                    {
                        ["ChipID"] = group.ChipID,
                        ["Plant"] = group.Plant,
                        ["Module"] = group.Module,
                        ["UserName"] = group.UserName,
                        ["MachineID"] = group.MachineID,
                        ["Operation"] = group.Operation,
                        ["DailyTarget"] = dailyTarget,
                        ["Shift"] = group.Shift,
                        ["ShiftDuration"] = shiftDuration
                    };

                    // Add date columns
                    var currentDate = startDate;
                    while (currentDate <= endDate)
                    {
                        var dayData = group.DailyData.FirstOrDefault(d => d.Date.Date == currentDate.Date);
                        var value = 0;

                        if (dayData != null)
                        {
                            if (showEfficiency)
                            {
                                // Calculate efficiency using dynamic shift duration
                                var smvRow = smvRowForGroup; // reuse group's mapping
                                var sewSmv = smvRow?.sew ?? 1;
                                var maxValue = Math.Ceiling((3300.0 / (sewSmv > 0 ? sewSmv : 1)) * 0.8) * shiftDuration; // Dynamic shift duration, 80% efficiency
                                var efficiency = maxValue > 0 ? (dayData.DailySum / maxValue) * 100.0 : 0.0;
                                value = (int)Math.Round(efficiency, 2);
                            }
                            else
                            {
                                value = dayData.DailySum;
                            }
                        }

                        rowData[currentDate.ToString("yyyy-MM-dd")] = value;
                        currentDate = currentDate.AddDays(1);
                    }

                    result.Add(rowData);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching daily data: {Message}", ex.Message);
                return Json(new { error = ex.Message });
            }
        }

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

            if (now.TimeOfDay > new TimeSpan(8, 25, 0) && now.TimeOfDay <= new TimeSpan(12, 45, 0))
            {
                elapsedSeconds -= 1200;
            }
            else if (now.TimeOfDay > new TimeSpan(12, 45, 0) && now.TimeOfDay <= new TimeSpan(15, 30, 0))
            {
                elapsedSeconds -= 3000;
            }
            else if (now.TimeOfDay > new TimeSpan(15, 30, 0) && now.TimeOfDay <= new TimeSpan(17, 25, 0))
            {
                elapsedSeconds -= 3600;
            }

            return elapsedSeconds;
        }

        /// <summary>
        /// Calculates the duration of a shift in hours based on the shift time string format.
        /// Handles both regular shifts (e.g., "06:00-22:00") and overnight shifts (e.g., "22:00-06:00").
        /// </summary>
        /// <param name="shiftTime">Shift time string in format "HH:mm-HH:mm"</param>
        /// <returns>Duration of the shift in hours</returns>
        private double CalculateShiftDuration(string shiftTime)
        {
            if (string.IsNullOrEmpty(shiftTime) || !shiftTime.Contains("-"))
            {
                // Default to 16 hours if shift time is invalid
                return 16.0;
            }

            try
            {
                var parts = shiftTime.Split('-');
                if (parts.Length != 2)
                {
                    return 16.0; // Default fallback
                }

                var startTimeStr = parts[0].Trim();
                var endTimeStr = parts[1].Trim();

                // Parse start and end times
                if (!TimeSpan.TryParse(startTimeStr, out var startTime) || 
                    !TimeSpan.TryParse(endTimeStr, out var endTime))
                {
                    return 16.0; // Default fallback
                }

                // Calculate duration
                double duration;
                if (endTime > startTime)
                {
                    // Regular shift (e.g., 06:00-22:00)
                    duration = (endTime - startTime).TotalHours;
                }
                else
                {
                    // Overnight shift (e.g., 22:00-06:00)
                    // Add 24 hours to end time to get the correct duration
                    duration = (endTime.Add(TimeSpan.FromHours(24)) - startTime).TotalHours;
                }

                return duration;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating shift duration for '{ShiftTime}', using default 16 hours", shiftTime);
                return 16.0; // Default fallback
            }
        }

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
                                                               r.Module == deleteData.module &&
                                                               r.Operation == deleteData.operation &&
                                                               r.Plant == deleteData.plant &&
                                                               r.Date == now);

                _logger.LogInformation($"Generated SQL query: {rowsToDeleteQuery.ToQueryString()}");

                var rowsToDelete = await rowsToDeleteQuery.ToListAsync();

                foreach (var row in rowsToDelete)
                {
                    _logger.LogInformation($"Row found: ID={row.ID}, ChipID={row.ChipID}, Style={row.style}, MAC={row.MAC}, UserName={row.UserName}, Module={row.Module}, Operation={row.Operation}, Date={row.Date}");

                    // Logging each column value individually to identify the cause of InvalidCastException
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

                if (!rowsToDelete.Any())
                {
                    return Json(new { success = false, message = "No rows found to delete." });
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
                    _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                _logger.LogWarning("Invalid data received: {Errors}", errors);
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class DeleteIotDataRequest
        {
            public string chipid { get; set; }
            public string style { get; set; }
            public string mac { get; set; }
            public string username { get; set; }
            public string module { get; set; }
            public string plant { get; set; }
            public string operation { get; set; }
        }

        public class AppDbContext52 : DbContext
        {
            public DbSet<IoTHOut> IoTHOut { get; set; }

            public AppDbContext52(DbContextOptions<AppDbContext52> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<IoTHOut>().HasNoKey();
            }
        }

        public class AppDbContext53 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext53(DbContextOptions<AppDbContext53> options) : base(options)
            {
            }
        }

        public class AppDbContext54 : DbContext
        {
            public DbSet<UserSMV> KreedIot_UserSMV { get; set; }

            public AppDbContext54(DbContextOptions<AppDbContext54> options) : base(options)
            {
            }
        }
        public class AppDbContext55 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext55(DbContextOptions<AppDbContext55> options) : base(options)
            {
            }
        }

        public class AppDbContext82 : DbContext
        {
            public DbSet<KreedaIot_DailySum> KreedaIot_DailySum { get; set; }

            public AppDbContext82(DbContextOptions<AppDbContext82> options) : base(options)
            {
            }
        }
    }
}
