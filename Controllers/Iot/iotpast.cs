using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models.AMR;
using Iot_dashboard.Models;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling historical IoT data operations
    /// </summary>
    public class iotpast : Controller
    {
        /// <summary>
        /// Database context for managing IoT test data
        /// </summary>
        public class AppDbContext42 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext42(DbContextOptions<AppDbContext42> options) : base(options)
            {
            }
        }

        private readonly AppDbContext42 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT test data</param>
        public iotpast(AppDbContext42 dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves historical IoT data for a specific module and time period
        /// </summary>
        /// <param name="date">The date to retrieve data for</param>
        /// <param name="module">The module to filter by (or "All" for all modules)</param>
        /// <param name="plant">The plant to filter by</param>
        /// <param name="time">The time range in format "HH:mm-HH:mm"</param>
        /// <param name="style">The style to filter by (optional)</param>
        /// <returns>Returns JSON containing historical IoT data grouped by user</returns>
        [HttpGet]
        public IActionResult FOF1(string date, string module, string plant, string time, string style)
        {
            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);
            int currentHour = sriLankanTime.Hour;

            // Validate date format
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            // Validate time range format
            var timeParts = time.Split('-');
            if (timeParts.Length != 2)
            {
                return BadRequest("Invalid time range format.");
            }

            if (!TimeSpan.TryParse(timeParts[0], out TimeSpan startTime) || !TimeSpan.TryParse(timeParts[1], out TimeSpan endTime))
            {
                return BadRequest("Invalid time range format.");
            }

            // Build query with filters
            var query = _dbContext.KreedaIotTestNew
                .Where(x => x.Date == parsedDate && x.Plant == plant && x.Time >= startTime && x.Time <= endTime &&
                            x.MachineID != null && x.DeltaTime != null && x.TotalRuntime != null && x.UserName != null);

            // Apply optional module filter
            if (module != "All")
            {
                query = query.Where(x => x.Module == module);
            }

            // Apply optional style filter
            if (!string.IsNullOrEmpty(style))
            {
                query = query.Where(x => x.style == style);
            }

            // Process and group the data
            var latestData = query
                .ToList()
                .GroupBy(x => x.UserName)
                .Select(group => new
                {
                    UserName = group.Key,
                    MachineID = group.First().MachineID,
                    deltaAvg = Math.Round(GetMostFrequentRange(group.Select(x => (int)Math.Round(x.DeltaTime / 1000.0))).Item3),
                    runAvg = Math.Round(GetMostFrequentRange(group.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3)
                })
                .Select(data => new
                {
                    UserName = $"{data.UserName}-{data.MachineID}",
                    data.deltaAvg,
                    data.runAvg
                })
                .ToList();

            var response = new
            {
                latestData
            };

            return Json(response);
        }

        /// <summary>
        /// Extracts the chip part from a chip ID
        /// </summary>
        /// <param name="chipID">The chip ID to extract from</param>
        /// <returns>Returns the chip part or empty string if invalid</returns>
        private string ExtractChipPart(string chipID)
        {
            if (string.IsNullOrEmpty(chipID))
                return string.Empty;

            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        /// <summary>
        /// Extracts the chip prefix from a chip ID
        /// </summary>
        /// <param name="chipID">The chip ID to extract from</param>
        /// <returns>Returns the chip prefix or empty string if invalid</returns>
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
        /// <returns>Returns the numeric part as an integer, or 0 if invalid</returns>
        private int ExtractNumericPart(string chipPrefix)
        {
            var numericPart = new string(chipPrefix.Where(char.IsDigit).ToArray());
            return int.TryParse(numericPart, out var number) ? number : 0;
        }

        /// <summary>
        /// Calculates the most frequent range of values within a dataset
        /// </summary>
        /// <param name="values">Collection of integer values to analyze</param>
        /// <returns>Returns a tuple containing (start, end, average) of the most frequent range</returns>
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
        /// Displays the historical IoT data interface
        /// </summary>
        /// <returns>Returns the historical IoT data view</returns>
        public IActionResult Index()
        {
            return View("~/Views/iotpast.cshtml");
        }
    }
}

