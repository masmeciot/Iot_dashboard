using Iot_dashboard.Models;
using Iot_dashboard.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Iot_dashboard.Controllers
{
    public class HomeController : Controller
    {
        public class AppDbContext : DbContext
        {
            public DbSet<KreedaIotTestNew> efficency { get; set; }

            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
            {
            }
        }
        public IActionResult Index()
        {
            return View();
        }
        private readonly AppDbContext _dbContext;
        private readonly IHubContext<DashboardHub> _hubContext;

        public HomeController(AppDbContext dbContext, IHubContext<DashboardHub> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAverageEfficiency()
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime yesterday = today.AddDays(-1);
                Console.WriteLine($"Retrieved {today}");
                Console.WriteLine($"Retrieved {yesterday}");

                DateTime now = DateTime.Now;
                Console.WriteLine($"Current Hour: {now.Hour}");

                int currentHour = now.Hour;
                Console.WriteLine($"Current Hour: {currentHour}");

                // Retrieve data with non-zero count for today and yesterday
                var todayDataNonZeroCount = await _dbContext.efficency
                    .Where(e => e.Plant == "MEC" && EF.Functions.DateDiffDay(e.Date, today) == 0 && e.count != 0)
                    .ToListAsync();

                var yesterdayDataNonZeroCount = await _dbContext.efficency
                    .Where(e => e.Plant == "MEC" && EF.Functions.DateDiffDay(e.Date, yesterday) == 0 && e.count != 0)
                    .ToListAsync();

                var todayEfficiencyValuesNonZeroCount = todayDataNonZeroCount
                    .Select(e => e.efficency)
                    .ToList();

                var yesterdayEfficiencyValuesNonZeroCount = yesterdayDataNonZeroCount
                    .Select(e => e.efficency)
                    .ToList();

                var distinctChipCountsNonZeroCount = todayDataNonZeroCount
                    .Select(e => e.ChipID)
                    .Distinct()
                    .Count();

                var countSumNonZeroCount = todayDataNonZeroCount
                    .Select(e => e.count)
                    .Sum();

                var todayPerformanceNonZeroCount = todayDataNonZeroCount
                    .Select(e => e.performance)
                    .ToList();

                var averageTodayEfficiencyNonZeroCount = todayEfficiencyValuesNonZeroCount.DefaultIfEmpty(0).Average();
                var averageYesterdayEfficiencyNonZeroCount = yesterdayEfficiencyValuesNonZeroCount.DefaultIfEmpty(0).Average();
                var efficiencyDeviationNonZeroCount = averageTodayEfficiencyNonZeroCount - averageYesterdayEfficiencyNonZeroCount;

                var averageTodayPerformanceNonZeroCount = todayPerformanceNonZeroCount.DefaultIfEmpty(0).Average();
                var averageYesterdayPerformanceNonZeroCount = yesterdayDataNonZeroCount
                    .Select(e => e.performance)
                    .DefaultIfEmpty(0)
                    .Average();
                var performanceDeviationNonZeroCount = averageTodayPerformanceNonZeroCount - averageYesterdayPerformanceNonZeroCount;

                // Calculate the deviation for pieces count excluding zero count entries
                var countYesterdayNonZeroCount = yesterdayDataNonZeroCount
                    .Select(e => e.count)
                    .Sum();
                var countDeviationNonZeroCount = countSumNonZeroCount - countYesterdayNonZeroCount;

                // Prediction calculation for the entire day based on average count per hour
                var averageCountPerHour = todayDataNonZeroCount
                    .GroupBy(e => e.Hour)
                    .Select(group => new
                    {
                        Hour = group.Key,
                        AverageCount = group.Average(e => e.count)
                    })
                    .ToList();

                var predictedCountSumForDay = averageCountPerHour.Sum(entry => entry.AverageCount) * (24 - currentHour);

                // Hourly count for the current hour
                var hourlyDataForToday = await _dbContext.efficency
                    .Where(e => e.Plant == "MEC" && e.Hour == now.Hour && EF.Functions.DateDiffDay(e.Date, today) == 0)
                    .ToListAsync();

                var countSumForHourToday = hourlyDataForToday
                    .Select(e => e.count)
                    .Sum();

                // Log or debug the data here
                Console.WriteLine($"Retrieved {todayDataNonZeroCount.Count} non-zero count records for today from the database.");
                Console.WriteLine($"Average Efficiency for Today (Non-Zero Count): {averageTodayEfficiencyNonZeroCount}");
                Console.WriteLine($"Sum of Count (Non-Zero Count): {countSumNonZeroCount}");
                Console.WriteLine($"Machines (Non-Zero Count): {distinctChipCountsNonZeroCount}");
                Console.WriteLine($"Efficiency Deviation from Yesterday (Non-Zero Count): {efficiencyDeviationNonZeroCount}");
                Console.WriteLine($"Performance Deviation from Yesterday (Non-Zero Count): {performanceDeviationNonZeroCount}");
                Console.WriteLine($"Pieces Count Deviation from Yesterday (Non-Zero Count): {countDeviationNonZeroCount}");
                Console.WriteLine($"Predicted Count Sum for the Entire Day (Non-Zero Count): {predictedCountSumForDay}");
                Console.WriteLine($"Pieces Count hour (Non-Zero Count): {countSumForHourToday}");

                // Send the data to clients using SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveEfficiencyUpdate", new
                {
                    AverageEfficiency = averageTodayEfficiencyNonZeroCount,
                    CountSum = countSumNonZeroCount,
                    Performance1 = averageTodayPerformanceNonZeroCount,
                    DistinctChipCounts = distinctChipCountsNonZeroCount,
                    EfficiencyDeviation = efficiencyDeviationNonZeroCount,
                    PerformanceDeviation = performanceDeviationNonZeroCount,
                    CountDeviation = countDeviationNonZeroCount,
                    CountSumForHour = countSumForHourToday,
                    PredictedCountSumForDay = predictedCountSumForDay
                });

                return Json(new
                {
                    AverageEfficiency = averageTodayEfficiencyNonZeroCount,
                    CountSum = countSumNonZeroCount,
                    Performance1 = averageTodayPerformanceNonZeroCount,
                    DistinctChipCounts = distinctChipCountsNonZeroCount,
                    EfficiencyDeviation = efficiencyDeviationNonZeroCount,
                    PerformanceDeviation = performanceDeviationNonZeroCount,
                    CountDeviation = countDeviationNonZeroCount,
                    CountSumForHour = countSumForHourToday,
                    PredictedCountSumForDay = predictedCountSumForDay
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error retrieving data from the database: {ex.Message}");

                // Return a more descriptive error response
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }



        [HttpGet]
        public IActionResult GetHourlyCountForToday()
        {
            try
            {
                DateTime today = DateTime.Today;

                var hourlyCounts = _dbContext.efficency
                    .Where(e => e.Plant == "MEC" && e.Date == today)
                    .GroupBy(e => e.Hour) 
                    .Select(group => new
                    {
                        Hour = group.Key,
                        CountSum = group.Sum(e => e.count) 
                    })
                    .OrderBy(item => item.Hour) 
                    .ToArray();

                var jsonData = JsonConvert.SerializeObject(hourlyCounts);
                return Content(jsonData, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from the database: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }



        [HttpGet]
        public IActionResult GetHourlyEffForToday()
        {
            try
            {
                DateTime today = DateTime.Today;

                var hourlyCounts = _dbContext.efficency
                    .Where(e => e.Plant == "MEC" && e.Date == today && e.count > 0) 
                    .GroupBy(e => e.Hour)
                    .Select(group => new
                    {
                        Hour = group.Key,
                        effSum = group.Average(e => e.efficency)
                    })
                    .OrderBy(item => item.Hour)
                    .ToArray();

                var jsonData = JsonConvert.SerializeObject(hourlyCounts);
                return Content(jsonData, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from the database: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetGroupedChartData()
        {
            try
            {
                var allDataForToday = await _dbContext.efficency
                    .Where(e => e.Date == DateTime.Today)
                    .ToListAsync();

                var moduleData = allDataForToday
                    .GroupBy(e => e.Module)
                    .Select(group => new
                    {
                        Module = group.Key,
                        CycleActAverage = group.Select(e => e.cycleAct).DefaultIfEmpty(0).Average() / 1000, 
                        CycleStandAverage = group.Select(e => e.cycleStand).DefaultIfEmpty(0).Average() / 1000, 
                        Efficiency = group.Select(e => e.efficency).DefaultIfEmpty(0).Average()
                    })
                    .ToList();

                var chartData = moduleData.Select(module => new
                {
                    Label = module.Module,
                    Data = new List<int> { (int)Math.Round(module.CycleActAverage), (int)Math.Round(module.CycleStandAverage) },
                    Efficiency = (int)Math.Round(module.Efficiency)
                }).ToList();

                return Json(chartData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving grouped chart data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetTop10Data()
        {
            try
            {
                DateTime today = DateTime.Today;

                var top10Data = await _dbContext.efficency
                    .Where(entry => entry.Plant == "MEC" && entry.Date == today && entry.count == 1)
                    .OrderByDescending(entry => entry.ID)
                    .Take(10)
                    .Select(entry => new
                    {
                        Module = entry.Module,
                        MachineID = entry.MachineID,
                        Operation = entry.Operation,
                        Time = entry.Time,
                        cycleStand = entry.cycleStand,
                        CycleAct = entry.cycleAct,
                        Efficiency = entry.efficency
                    })
                    .ToListAsync();

                return Json(top10Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving top 15 data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetuserData()
        {
            try
            {
                DateTime today = DateTime.Today;

                var userData = await _dbContext.efficency
                    .Where(entry => entry.Plant == "MEC" && entry.Date == today && entry.count > 0)
                    .GroupBy(entry => entry.UID)
                    .Select(group => new
                    {
                        UID = group.Key,
                        CountSum = group.Sum(entry => entry.count),
                        Efficiency = group.Average(entry => entry.efficency),
                        Data = group.Select(entry => new
                        {
                            Module = entry.Module,
                            Operation = entry.Operation,
                            UserName = entry.UserName,
                            UserID = entry.UID,
                            Count = entry.count
                        }).OrderByDescending(entry => entry.Count).FirstOrDefault()
                    })
                    .OrderByDescending(group => group.CountSum)
                    .ToListAsync();

                return Json(userData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }





        [HttpGet]
        public async Task<IActionResult> GetModuleData()
        {
            try
            {
                DateTime today = DateTime.Today;

                var userData = await _dbContext.efficency
                    .Where(entry => entry.Plant == "MEC" && entry.Date == today && entry.count > 0)
                    .GroupBy(entry => entry.Module)
                    .Select(group => new
                    {
                        Module = group.Key,
                        CountSum = group.Sum(entry => entry.count),
                        cycleAv = group.Average(entry => entry.cycleAct),
                        Efficiency = group.Average(entry => entry.efficency),
                        Data = group.Select(entry => new
                        {
                            Module = entry.Module,

                        }).FirstOrDefault()
                    })
                    .ToListAsync();

                return Json(userData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }








        [HttpPost]
        public async Task<IActionResult> UpdateEfficiency([FromBody] UpdateEfficiencyModel model)
        {
            try
            {
                var efficiencyPercentage = model.EfficiencyPercentage;

                // Broadcast the updated efficiency to all clients
                await _hubContext.Clients.All.SendAsync("ReceiveEfficiencyUpdate", efficiencyPercentage);

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error updating efficiency: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }

        }


        [Table("KreedaIotTestNew")]
        public class UpdateEfficiencyModel
        {
            public decimal EfficiencyPercentage { get; set; }
        }

    }
}