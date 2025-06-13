using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Iot_dashboard.Controllers.Synergy.synbest;

namespace Iot_dashboard.Controllers.Synergy
{
    /// <summary>
    /// Controller responsible for handling best performance data for both hand and sewing operations
    /// </summary>
    public class bestboth : Controller
    {
        /// <summary>
        /// Displays the best performance interface for both hand and sewing operations
        /// </summary>
        /// <returns>Returns the best performance view</returns>
        public IActionResult Index()
        {
            return View("~/Views/synergy/bestboth.cshtml");
        }

        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext73 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext73(DbContextOptions<AppDbContext73> options) : base(options)
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
        public class AppDbContext74 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext74(DbContextOptions<AppDbContext74> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }

        private readonly AppDbContext73 _dbContext;
        private readonly AppDbContext74 _dbContext1;

        /// <summary>
        /// Constructor that initializes both database contexts
        /// </summary>
        /// <param name="dbContext">Context for IoT dashboard data</param>
        /// <param name="dbContext1">Context for tack time data</param>
        public bestboth(AppDbContext73 dbContext, AppDbContext74 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }

        /// <summary>
        /// Retrieves the best performance data for both hand and sewing operations for the current hour
        /// </summary>
        /// <returns>Returns JSON containing tack time and latest performance data</returns>
        [HttpGet]
        public IActionResult Data()
        {
            // Fetch the current time in Sri Lankan time zone
            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);
            int currentHour = sriLankanTime.Hour;

            // Fetch tackValue from the database for module "SYN33"
            var tackValue = _dbContext1.KreedaIot_TackTime
                            .Where(v => v.Module == "SYN26")
                            .Select(v => v.Tack)
                            .FirstOrDefault();

            // Fetch the latest data grouped by UserName
            var latestData = _dbContext.iot
                .Where(x => x.Plant == "SYNERGY" && x.Hour == currentHour && x.DeltaTime <= 70000)
                .ToList()
                .GroupBy(x => x.ChipID)
                .Select(group => new
                {
                    UserName = group.Key,
                    ChipID = group.First().ChipID,  // Get ChipID
                    Hand = group.First().Hand,
                    Sew = group.First().Sew,
                    MachineID = group.First().MachineID,
                    deltaAvg = Math.Round(GetMostFrequentRange(group.Select(x => (int)Math.Round(x.DeltaTime / 1000.0))).Item3),
                    runAvg = Math.Round(GetMostFrequentRange(group.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3),
                })
                .ToList()
                // Extract ChipID part and order by the numeric part of the ChipID prefix
                .OrderBy(data => ExtractNumericPart(ExtractChipPrefix(data.ChipID)))
                .Select(data => new
                {
                    UserName = ExtractChipPart(data.ChipID),  // Extract the part of ChipID
                    data.Hand,
                    data.Sew,
                    data.deltaAvg,
                    data.runAvg
                })
                .ToList();

            var response = new
            {
                tackValue,
                latestData
            };

            // Return the JSON response
            return Json(response);
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
        /// Extracts the numeric part from a module name
        /// </summary>
        /// <param name="moduleName">The module name to extract from</param>
        /// <returns>Returns the numeric part of the module name</returns>
        private int ExtractNumericPart(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return 0;

            // Extract only the numeric part of the module name
            var numericPart = new string(moduleName.Where(char.IsDigit).ToArray());

            return int.TryParse(numericPart, out var number) ? number : 0;
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
            return parts.Length > 0 ? parts[0] : string.Empty; // Extract the part before "/"
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
    }
}
