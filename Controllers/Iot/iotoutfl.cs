using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Iot
{
    public class iotoutfl : Controller
    {
        public class AppDbContext43 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext43(DbContextOptions<AppDbContext43> options) : base(options)
            {
            }
        }

        public class AppDbContext44 : DbContext
        {
            public DbSet<UserSMV> KreedIot_UserSMV { get; set; }

            public AppDbContext44(DbContextOptions<AppDbContext44> options) : base(options)
            {
            }
        }

        private readonly AppDbContext43 _dbContext;
        private readonly AppDbContext44 _dbContext1;

        public iotoutfl(AppDbContext43 dbContext, AppDbContext44 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }

        [HttpGet]
        public async Task<IActionResult> FOF1(string date, string module, string plant, string style)
        {
            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);
            int currentHour = sriLankanTime.Hour;

            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            var query = _dbContext.KreedaIotTestNew
                .Where(x => x.Date == parsedDate && x.Plant == plant && x.MachineID != null && x.UserName != null);

            if (module != "All")
            {
                query = query.Where(x => x.Module == module);
            }

            if (!string.IsNullOrEmpty(style))
            {
                query = query.Where(x => x.style == style);
            }

            var latestData = await query.ToListAsync();

            var groupedData = latestData
                .GroupBy(x => new { x.UserName, x.Operation, x.style })
                .Select(group => new
                {
                    Module = group.First().Module,
                    UserName = group.Key.UserName,
                    Operation = group.Key.Operation,
                    Style = group.Key.style,
                    MachineID = group.First().MachineID,
                    HourlyCounts = new int[]
                    {
                        group.Count(x => x.Time >= new TimeSpan(7, 25, 0) && x.Time < new TimeSpan(8, 50, 0)),
                        group.Count(x => x.Time >= new TimeSpan(8, 50, 0) && x.Time < new TimeSpan(9, 50, 0)),
                        group.Count(x => x.Time >= new TimeSpan(9, 50, 0) && x.Time < new TimeSpan(10, 50, 0)),
                        group.Count(x => x.Time >= new TimeSpan(10, 50, 0) && x.Time < new TimeSpan(11, 50, 0)),
                        group.Count(x => x.Time >= new TimeSpan(11, 50, 0) && x.Time < new TimeSpan(13, 20, 0)),
                        group.Count(x => x.Time >= new TimeSpan(13, 20, 0) && x.Time < new TimeSpan(14, 20, 0)),
                        group.Count(x => x.Time >= new TimeSpan(14, 20, 0) && x.Time < new TimeSpan(15, 20, 0)),
                        group.Count(x => x.Time >= new TimeSpan(15, 20, 0) && x.Time < new TimeSpan(16, 30, 0)),
                        group.Count(x => x.Time >= new TimeSpan(16, 30, 0) && x.Time < new TimeSpan(17, 30, 0))
                    }
                })
                .ToList();

            var userSmvs = await _dbContext1.KreedIot_UserSMV
                .GroupBy(u => new { u.UserName, u.style, u.Operation })
                .Select(group => new
                {
                    UserName = group.Key.UserName,
                    Style = group.Key.style,
                    Operation = group.Key.Operation,
                    SumHandSew = group.Sum(g => g.Hand + g.sew)
                })
                .ToDictionaryAsync(u => (u.UserName, u.Style, u.Operation), u => u.SumHandSew);

            var utilizationData = groupedData.Select(data => new
            {
                Module = data.Module,
                Operation = data.Operation,
                Machine = data.MachineID,
                User = data.UserName,
                TargetCount = data.HourlyCounts.Sum(),
                HourlyCounts = data.HourlyCounts,
                Utilization = CalculateUtilization(data.UserName, data.Style, data.Operation, data.HourlyCounts, userSmvs)
            }).ToList();

            var response = new
            {
                utilizationData
            };

            return Json(response);
        }

        private string CalculateUtilization(string userName, string style, string operation, int[] hourlyCounts, Dictionary<(string, string, string), int> userSmvs)
        {
            var sumOfH = hourlyCounts.Sum();
            var key = (userName, style, operation);
            var sumOfHandSew = userSmvs.ContainsKey(key) ? userSmvs[key] : 0;
            var summ = sumOfHandSew * sumOfH;

            var workingSeconds = CalculateWorkingSeconds();

            var utilization = workingSeconds > 0 ? (int)Math.Round((summ / (double)workingSeconds) * 100) : 0;

            return utilization.ToString("F0") + "%";
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
                elapsedSeconds -= 1200; // 20 minutes
            }
            else if (now.TimeOfDay > new TimeSpan(12, 45, 0) && now.TimeOfDay <= new TimeSpan(15, 30, 0))
            {
                elapsedSeconds -= 3000; // 50 minutes
            }
            else if (now.TimeOfDay > new TimeSpan(15, 30, 0) && now.TimeOfDay <= new TimeSpan(17, 25, 0))
            {
                elapsedSeconds -= 3600; // 60 minutes
            }

            return elapsedSeconds;
        }

        public IActionResult Index()
        {
            return View("~/Views/IoToutfilter.cshtml");
        }
    }
}
