using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Synergy
{
    /// <summary>
    /// Controller responsible for handling best performance data for Synergy modules
    /// </summary>
    public class synbest : Controller
    {
        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext60 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext60(DbContextOptions<AppDbContext60> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }

        /// <summary>
        /// Database context for managing tack time data
        /// </summary>
        public class AppDbContext61 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext61(DbContextOptions<AppDbContext61> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }

        private readonly AppDbContext60 _dbContext;
        private readonly AppDbContext61 _dbContext1;

        /// <summary>
        /// Constructor that initializes both database contexts
        /// </summary>
        /// <param name="dbContext">Context for IoT dashboard data</param>
        /// <param name="dbContext1">Context for tack time data</param>
        public synbest(AppDbContext60 dbContext, AppDbContext61 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }

        /// <summary>
        /// Retrieves the best performance data for all Synergy modules for the current day
        /// </summary>
        /// <returns>Returns JSON containing tack time and latest performance data for all modules</returns>
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
                                RunAvg = Math.Round(GetMostFrequentRange(g.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3),
                                Sew = x.Sew
                            })
                            .OrderBy(user => GetCustomOrder(user.UserName)) // Custom order for userName
                            .ToArray()
                        };
                    }
                );

            return Json(result);
        }

        /// <summary>
        /// Helper method to determine custom sorting order for user names
        /// </summary>
        /// <param name="userName">The user name to sort</param>
        /// <returns>Returns a numeric value used for sorting user names</returns>
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

        /// <summary>
        /// Extracts the part of the chip ID before the "/" character
        /// </summary>
        /// <param name="chipID">The complete chip ID</param>
        /// <returns>Returns the extracted part of the chip ID</returns>
        private string ExtractChipPart(string chipID)
        {
            if (string.IsNullOrEmpty(chipID))
                return string.Empty;

            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        /// <summary>
        /// Extracts the prefix part of the chip ID
        /// </summary>
        /// <param name="chipID">The complete chip ID</param>
        /// <returns>Returns the prefix part of the chip ID</returns>
        private string ExtractChipPrefix(string chipID)
        {
            if (string.IsNullOrEmpty(chipID))
                return string.Empty;

            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        /// <summary>
        /// Extracts the numeric part from a chip prefix
        /// </summary>
        /// <param name="chipPrefix">The chip prefix to extract from</param>
        /// <returns>Returns the numeric part of the chip prefix</returns>
        private int ExtractNumericPart(string chipPrefix)
        {
            var numericPart = new string(chipPrefix.Where(char.IsDigit).ToArray());
            return int.TryParse(numericPart, out var number) ? number : 0;
        }

        /// <summary>
        /// Calculates the most frequent range of values from a collection of integers
        /// </summary>
        /// <param name="values">Collection of integer values to analyze</param>
        /// <returns>Returns a tuple containing the start, end, and average of the most frequent range</returns>
        private (int, int, double) GetMostFrequentRange(IEnumerable<int> values)
        {
            if (!values.Any()) return (0, 0, 0);

            var sortedValues = values.OrderBy(x => x).ToList();
            var clusters = new List<(int start, int end, int count, double average)>();

            int start = sortedValues[0];
            int end = start;
            double sum = start;
            int count = 1;

            for (int i = 1; i < sortedValues.Count; i++)
            {
                if (sortedValues[i] - sortedValues[i - 1] <= 3)
                {
                    end = sortedValues[i];
                    sum += sortedValues[i];
                    count++;
                }
                else
                {
                    clusters.Add((start, end, count, sum / count));
                    start = sortedValues[i];
                    end = start;
                    sum = start;
                    count = 1;
                }
            }

            clusters.Add((start, end, count, sum / count));

            var mostFrequentCluster = clusters.OrderByDescending(c => c.count).First();
            return (mostFrequentCluster.start, mostFrequentCluster.end, mostFrequentCluster.average);
        }

        /// <summary>
        /// Displays the best performance interface
        /// </summary>
        /// <returns>Returns the best performance view</returns>
        public IActionResult Index()
        {
            return View("~/Views/synergy/synbest.cshtml");
        }
    }
}
