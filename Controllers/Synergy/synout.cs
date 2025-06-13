using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Synergy
{
    public class synout : Controller
    {
        private readonly AppDbContext52 _dbContext;
        private readonly AppDbContext53 _dbContext1;
        private readonly AppDbContext54 _dbContext2;
        private readonly AppDbContext55 _dbContext3;

        private readonly ILogger<synout> _logger;
        public synout(AppDbContext52 dbContext, AppDbContext53 dbContext1, AppDbContext54 dbContext2, AppDbContext55 dbContext3, ILogger<synout> logger)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
            _dbContext3 = dbContext3; 
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Views/synout.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetIotData()
        {
            var tacks = await _dbContext1.KreedaIot_TackTime
                                         .Where(t => t.Module == "SYN33")
                                         .ToListAsync();

            if (tacks == null )
            {
                return NotFound("Tack times not found.");
            }

            var tackDict = tacks.ToDictionary(t => t.Module, t => t.Tack);

            var target = 3600 / tackDict["SYN33"];
         

            var iotData = await _dbContext.SynOut
                                           .Select(i => new
                                           {
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
                                               i.MAC,
                                               Target = target
                                           })
                                           .ToListAsync();

          /*  var userSmvs = await _dbContext2.KreedIot_UserSMV
                                            .Select(u => new
                                            {
                                                u.UserName,
                                                u.style,
                                                SumHandSew = u.Hand + u.sew,
                                                u.Operation,
                                            })
                                             .ToDictionaryAsync(u => (u.UserName, u.style, u.Operation), u => u.SumHandSew);*/

            var result = iotData.Select(i =>
            {

                var sumOfH =i.H0+ i.H1 + i.H2 + i.H3 + i.H4 + i.H5 + i.H6 + i.H7 + i.H8 + i.H9+i.H10+i.H11+i.H12+i.H13+i.H14+i.H15+i.H16+i.H17+i.H18+i.H19+i.H20+i.H21+i.H22+i.H23;
                //var key = (i.UserName, i.style, i.Operation); // Use all three keys
             //   var sumOfHandSew = userSmvs.ContainsKey(key) ? userSmvs[key] : 0;
              //  var summ = sumOfHandSew * sumOfH;

              //  var workingSeconds = CalculateWorkingSeconds();


                // Console.WriteLine($"User: {i.UserName}, Sum of H: {sumOfH}, Sum of HandSew: {sumOfHandSew}, Summ: {summ}, WorkingSeconds: {workingSeconds}");

                //var utilization = workingSeconds > 0 ? (int)Math.Round((summ / (double)workingSeconds) * 100) : 0;




                return new
                {
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
                   i.Target,
                    sum = sumOfH,
                    i.MAC,
                    i.style,
                    i.MachineID,
                    i.Plant,
                   // Utilization = utilization.ToString("F0") + "%"
                };
            }).ToList();

            return Json(result);
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
                                                              // r.UserName == deleteData.username &&
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
            public DbSet<SynOut> SynOut { get; set; }

            public AppDbContext52(DbContextOptions<AppDbContext52> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<SynOut>().HasNoKey();
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
    }
}
