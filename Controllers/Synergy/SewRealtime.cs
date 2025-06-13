using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Iot_dashboard.Controllers.Synergy.realtime;

namespace Iot_dashboard.Controllers.Synergy
{
    public class sewrealtime : Controller
    {

        public class AppDbContext69: DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext69(DbContextOptions<AppDbContext69> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }


        public class AppDbContext70 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext70(DbContextOptions<AppDbContext70> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
               
            }
        }


        private readonly AppDbContext69 _dbContext;
        private readonly AppDbContext70 _dbContext1;

        public sewrealtime(AppDbContext69 dbContext, AppDbContext70 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }


        [HttpGet]
        public IActionResult Data()
        {
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, sriLankaTimeZone).Date;

            // Fetch data for the specific condition
            var filteredData = _dbContext.iot
                .Where(x => x.Plant == "SYNERGY" && x.TotalRuntime <= 70000 && x.DeltaTime <= 60000 && x.Date == now)
                .ToList();

            // Get the most recent style for each module
            var recentStyles = filteredData
                .GroupBy(x => x.Module) // Group by Module
                .Select(moduleGroup =>
                {
                    // Find the most recent record for the module
                    var mostRecentRecord = moduleGroup
                        .OrderByDescending(x => x.Time) // Assuming `Time` indicates recency
                        .FirstOrDefault();

                    return new
                    {
                        Module = moduleGroup.Key,
                        Style = mostRecentRecord?.style
                    };
                })
                .ToList();

            // Filter the data for each module to include only rows with the most recent style
            var mostRecentStyleData = filteredData
                .Where(x => recentStyles.Any(rs => rs.Module == x.Module && rs.Style == x.style))
                .ToList();

            // Perform further grouping and transformations
            var groupedData = mostRecentStyleData
                .GroupBy(x => new { x.style, x.UserName, x.Operation })
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList();

            // Fetch tack values for all modules
            var tackValues = _dbContext1.KreedaIot_TackTime
                .ToDictionary(t => t.Module, t => t.Tack);

            var result = groupedData
                .GroupBy(x => x.Module)
                .OrderBy(g => ExtractNumericPart(g.Key))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var tack = tackValues.TryGetValue(g.Key, out var tackValue) ? tackValue : 0;

                        return new
                        {
                            Tack = tack, // Include tack value
                            Data = g.Select(x => new
                            {
                                UserName = ExtractChipPart(x.ChipID),
                                RunAvg = Math.Round(x.TotalRuntime / 1000.0, 0),
                                Sew = x.Sew
                            })
                            .OrderBy(user => GetCustomOrder(user.UserName)) // Custom order for userName
                            .ToArray()
                        };
                    }
                );

            return Json(result);
        }



        // Helper method to extract numeric and alphabetical parts for sorting
        private static int GetCustomOrder(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return int.MaxValue; // Place empty or null values at the end

            // Extract the alphabetic prefix (e.g., "D" or "M")
            string prefix = new string(userName.TakeWhile(char.IsLetter).ToArray());

            // Extract the numeric part (e.g., "01", "02")
            string numericPart = new string(userName.SkipWhile(char.IsLetter).ToArray());

            if (int.TryParse(numericPart, out int number))
            {
                // Combine alphabetic prefix and numeric part for sorting
                return prefix[0] * 10000 + number; // Ensures `D` values come before `M`
            }

            return int.MaxValue; // Place invalid values at the end
        }




        // Method to extract the numeric part from module name
        private int ExtractNumericPart(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return 0;

            // Extract only the numeric part of the module name
            var numericPart = new string(moduleName.Where(char.IsDigit).ToArray());

            return int.TryParse(numericPart, out var number) ? number : 0;
        }

        // Extract the chip part (e.g., before the "/")
        private string ExtractChipPart(string chipID)
        {
            if (string.IsNullOrEmpty(chipID))
                return string.Empty;

            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : string.Empty; // Extract the part before "/"
        }

        // New method to extract the prefix (e.g., M01) from the ChipID
        private string ExtractChipPrefix(string chipID)
        {
            if (string.IsNullOrEmpty(chipID))
                return string.Empty;

            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : string.Empty; // Return the prefix (e.g., M01)
        }



        public IActionResult Index()
        {
            return View("~/Views/synergy/sewRealtime.cshtml");
        }
    }
}
