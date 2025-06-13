using Iot_dashboard.Hubs;
using Iot_dashboard.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Iot_dashboard.Controllers.andon
{
    public class andonController : Controller
    {
        public class AppDbContext1 : DbContext
        {
            public DbSet<KreedaIot_Andon_raised> table { get; set; }

            public AppDbContext1(DbContextOptions<AppDbContext1> options) : base(options)
            {
            }
        }
        private readonly AppDbContext1 _dbContext;
        private readonly IHubContext<andonHub> _hubContext;

        public andonController(AppDbContext1 dbContext, IHubContext<andonHub> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
        }
        public IActionResult Index()
        {
            return View("~/Views/andon/andon.cshtml");
        }


        [HttpGet]
        public async Task<IActionResult> CalculateResponseRate()
        {
            try
            {
                DateTime today = DateTime.Today;

                // Retrieve data for today
                var todayAttempts = await _dbContext.table.CountAsync(entry => entry.date_raised.Date == today.Date);
                var todayResponses = await _dbContext.table.CountAsync(entry => entry.date_raised.Date == today.Date && entry.status == "completed");

                // Calculate response rate directly from attempts to responses ratio
                double responseRate = todayResponses / (double)todayAttempts * 100;


                // Send data to frontend via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveResponseRate", responseRate);
                Console.WriteLine($"todayAttempts: {todayAttempts}");
                Console.WriteLine($"todayResponses: {todayResponses}");
                Console.WriteLine($"responseRate: {responseRate}");

                return Ok(new { ResponseRate = responseRate });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPendingAndonDataForToday()
        {
            try
            {
                DateTime today = DateTime.Today;

                var pendingAndonData = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == today && entry.status == "pending")
                    .OrderByDescending(entry => entry.ID)
                    .Select(entry => new
                    {
                        entry.ID,
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        DateRaised = entry.date_raised.ToString("yyyy-MM-dd"),

                        // Add 5 hours and 30 minutes to the time from the table
                        StartTime = (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)).ToString("HH:mm:ss"),

                        entry.status
                    })
                    .ToListAsync();

                return Ok(pendingAndonData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }





        [HttpGet]
        public async Task<IActionResult> GetPendingAndonDataForhistory()
        {
            try
            {
                DateTime today = DateTime.Today;

                var pendingAndonData = await _dbContext.table
                    .Where(entry => entry.date_raised.Date != today && entry.status == "pending")
                    .Select(entry => new
                    {
                        entry.ID,
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        DateRaised = entry.date_raised.ToString("yyyy-MM-dd"),
                        StartTime = (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)).ToString("HH:mm:ss"),
                        entry.status
                    })
                    .Take(3)
                    .ToListAsync();

                return Ok(pendingAndonData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> completedTdy()
        {
            try
            {
                DateTime today = DateTime.Today;

                var pendingAndonData = await _dbContext.table
                    .Where(entry => entry.status == "completed")
                    .OrderByDescending(entry => entry.andon_resolved_time)
                    .Select(entry => new
                    {
                        entry.ID,
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        DateRaised = entry.date_raised.ToString("yyyy-MM-dd"),
                        StartTime = (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)).ToString("HH:mm:ss"),
                        endTime = (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)).ToString("HH:mm:ss"),
                        entry.andon_resolved_time,
                        entry.resolved_by,
                        entry.status,
                        TimeGapMinutes = ((entry.andon_resolved_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)) - (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30))).TotalMinutes
                    })
                    .Take(3)
                    .ToListAsync();

                return Ok(pendingAndonData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetGroupedChartData()
        {
            try
            {
                var resolvedByCounts = await _dbContext.table
                    .Where(entry => entry.status == "completed")
                    .GroupBy(entry => entry.resolved_by)
                    .Select(group => new
                    {
                        label = group.Key,
                        count = group.Count()
                    })
                    .ToListAsync();

                return Ok(resolvedByCounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Getbytable()
        {
            try
            {

                var completedAndons = await _dbContext.table
                    .Where(entry => entry.status == "completed")
                    .ToListAsync();


                var resolvedByData = completedAndons
                    .GroupBy(entry => entry.resolved_by)
                    .Select(group => new
                    {
                        ResolvedBy = group.Key,
                        CompletedAndons = group.Count(),
                        ResponseRate = group.Count() * 100 / completedAndons.Count(),
                        AverageTime = group.Average(entry =>
                            entry.andon_resolved_time != null && entry.andon_start_time != null ?
                            (entry.andon_resolved_time - entry.andon_start_time).TotalMinutes : 0),
                        CommonIssues = string.Join(", ", group.Select(entry => entry.andon_issue).Distinct()),
                        Category = group.Select(entry => entry.andon_category).FirstOrDefault()
                    })
                    .ToList();

                return Ok(resolvedByData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }







        [HttpGet]
        public async Task<IActionResult> GetcategorytData()
        {
            try
            {
                var resolvedByCounts = await _dbContext.table
                    .Where(entry => entry.status == "completed")
                    .GroupBy(entry => entry.andon_category)
                    .Select(group => new
                    {
                        label = group.Key,
                        count = group.Count()
                    })
                    .ToListAsync();

                return Ok(resolvedByCounts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> CalculateAverageResponseTime()
        {
            try
            {
                DateTime today = DateTime.Today;

                var todayCompletedData = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == today.Date
                                    && entry.status == "completed"
                                    && entry.andon_start_time != null
                                    && entry.andon_resolved_time != null)
                    .ToListAsync();

                // Calculate average response time in minutes
                double averageResponseTimeMinutes = 0;
                if (todayCompletedData != null && todayCompletedData.Any())
                {
                    List<TimeSpan> responseTimes = new List<TimeSpan>();

                    foreach (var item in todayCompletedData)
                    {
                        if (item.andon_start_time != null && item.andon_resolved_time != null)
                        {
                            TimeSpan responseTime = item.andon_resolved_time - item.andon_start_time;
                            responseTimes.Add(responseTime);
                        }
                    }

                    if (responseTimes.Any())
                    {
                        double totalMinutes = responseTimes.Sum(time => time.TotalMinutes);
                        averageResponseTimeMinutes = totalMinutes / responseTimes.Count;
                    }
                }

                int pendingDataCountForToday = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == today.Date && entry.status == "pending")
                    .CountAsync();

                int comDataCountForToday = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == today.Date && entry.status == "completed")
                    .CountAsync();

                int pendingCount = await _dbContext.table
                    .Where(entry => entry.date_raised.Date != today.Date && entry.status == "pending")
                    .CountAsync();


                string mostRecentResolvedBy = await _dbContext.table
                .Where(entry => entry.status == "completed")
                .OrderByDescending(entry => entry.ID)
                .Select(entry => entry.resolved_by)
                .FirstOrDefaultAsync();

                // Send average response time to frontend via SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveAverageResponseTime", averageResponseTimeMinutes);
                Console.WriteLine($"Average Response Time (Minutes): {averageResponseTimeMinutes}");

                return Ok(new
                {
                    AverageResponseTimeMinutes = averageResponseTimeMinutes,
                    PendingDataCountForToday = pendingDataCountForToday,
                    ComDataCountForToday = comDataCountForToday,
                    PendingCount = pendingCount,
                    MostRecentResolvedBy = mostRecentResolvedBy
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }








    }
}