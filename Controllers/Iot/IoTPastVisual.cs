using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Iot
{
    public class IoTPastVisual : Controller
    {
        public class AppDbContext56 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext56(DbContextOptions<AppDbContext56> options) : base(options)
            {
            }
        }

        public class AppDbContext57 : DbContext
        {
            public DbSet<UserSMV> KreedIot_UserSMV { get; set; }

            public AppDbContext57(DbContextOptions<AppDbContext57> options) : base(options)
            {
            }
        }

        private readonly AppDbContext56 _dbContext;
        private readonly AppDbContext57 _dbContext1;

        public IoTPastVisual(AppDbContext56 dbContext, AppDbContext57 dbContext1)
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

            var latestData = await query
                .ToListAsync();

            var groupedData = latestData
                .GroupBy(x => new { x.UserName, x.Operation })
                .Select(group => new
                {
                    Module = group.First().Module,
                    UserName = group.Key.UserName,
                    Operation = group.Key.Operation,
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
                                              .Select(u => new
                                              {
                                                  u.UserName,
                                                  SumHandSew = u.Hand + u.sew
                                              })
                                              .ToDictionaryAsync(u => u.UserName, u => u.SumHandSew);

            var utilizationData = groupedData.Select(data => new
            {
                Module = data.Module,
                Operation = data.Operation,
                Machine = data.MachineID,
                User = data.UserName,
                TargetCount = data.HourlyCounts.Sum(),
                HourlyCounts = data.HourlyCounts,
                //Utilization = CalculateUtilization(data.UserName, data.HourlyCounts, userSmvs)
            }).ToList();

            var response = new
            {
                utilizationData
            };

            return Json(response);
        }


        private string CalculateUtilization(string userName, int[] hourlyCounts, Dictionary<string, int> userSmvs)
        {
            var sumOfH = hourlyCounts.Sum();
            var sumOfHandSew = userSmvs.ContainsKey(userName) ? userSmvs[userName] : 0;
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
            return View("~/Views/IoTPastVisual.cshtml");
        }
    }
}
