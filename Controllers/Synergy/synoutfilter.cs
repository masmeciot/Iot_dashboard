using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Iot_dashboard.Controllers.Iot.iotoutfl;

namespace Iot_dashboard.Controllers.Synergy
{
    public class synoutfilter : Controller
    {
        public class AppDbContext63 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext63(DbContextOptions<AppDbContext63> options) : base(options)
            {
            }
        }


        private readonly AppDbContext63 _dbContext;

        public synoutfilter(AppDbContext63 dbContext)
        {
            _dbContext = dbContext;
          
        }


        [HttpGet]
        public async Task<IActionResult> Data(string date, string module, string plant, string style)
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
                    HourlyCounts = Enumerable.Range(0, 24) // Generate counts for all 24 hours
                        .Select(hour => group.Count(x => x.Time.Hours == hour)) // Filter data by hour
                        .ToArray() // Convert the results into an array
                })
                .ToList();

            var utilizationData = groupedData.Select(data => new
            {
                Module = data.Module,
                Operation = data.Operation,
                Machine = data.MachineID,
                User = data.UserName,
                TargetCount = data.HourlyCounts.Sum(),
                HourlyCounts = data.HourlyCounts
            }).ToList();

            var response = new
            {
                utilizationData
            };

            return Json(response);
        }


        public IActionResult Index()
        {
            return View("~/Views/synergy/iotoutfiltersyn.cshtml");
        }
    }
}
