using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Synergy
{
    /// <summary>
    /// Controller responsible for handling Synergy module data operations and API endpoints
    /// </summary>
    public class ModuleAPI : Controller
    {
        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext62 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext62(DbContextOptions<AppDbContext62> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }

        /// <summary>
        /// Database context for managing IoT test data
        /// </summary>
        public class AppDbContext80 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext80(DbContextOptions<AppDbContext80> options) : base(options)
            {
            }
        }

        /// <summary>
        /// Database context for managing tack time data
        /// </summary>
        public class AppDbContext71 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext71(DbContextOptions<AppDbContext71> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
            }
        }

        private readonly AppDbContext80 _dbContext2;
        private readonly AppDbContext62 _dbContext;
        private readonly AppDbContext71 _dbContext1;

        /// <summary>
        /// Constructor that initializes all database contexts
        /// </summary>
        /// <param name="dbContext">Context for IoT dashboard data</param>
        /// <param name="dbContext1">Context for tack time data</param>
        /// <param name="dbContext2">Context for IoT test data</param>
        public ModuleAPI(AppDbContext62 dbContext, AppDbContext71 dbContext1, AppDbContext80 dbContext2)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
        }

        /// <summary>
        /// Retrieves the best performing data for a specific module in the current hour
        /// </summary>
        /// <param name="module">The module identifier</param>
        /// <returns>Returns JSON containing the best performing data and tack time for the module</returns>
        [HttpGet]
        [Route("api/best/data/{module}")]
        public IActionResult GetDataByModule(string module)
        {
            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);
            int currentHour = sriLankanTime.Hour;

            var latestData = _dbContext.iot
                .Where(x => x.Module == module &&
                            x.Hour == currentHour &&
                            x.TotalRuntime > 0 &&
                            x.DeltaTime <= 70000) 
                .ToList()
                 .GroupBy(x => x.ChipID)  
                 .Select(userGroup => new
                 {
                     UserName = ExtractChipPart(userGroup.First().ChipID),  
                     Hand = userGroup.First().Hand,
                     Sew = userGroup.First().Sew,
                     deltaAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.DeltaTime / 1000.0))).Item3),
                     runAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3)
                 })
                 .OrderBy(data => ExtractNumericPart(ExtractChipPrefix(data.UserName)))  
                 .ToList();

            var tack = _dbContext1.KreedaIot_TackTime
             .Where(t => t.Module == module)
             .Select(t => t.Tack)
             .FirstOrDefault();

            var result = new Dictionary<string, object>
            {
                { module, new { latestData, tack } }  
            };

            return Json(result);
        }

        /// <summary>
        /// Retrieves filtered data for a specific module within a date and time range
        /// </summary>
        /// <param name="module">The module identifier</param>
        /// <param name="date">The date to filter by</param>
        /// <param name="starttime">The start time of the range</param>
        /// <param name="endtime">The end time of the range</param>
        /// <returns>Returns JSON containing the filtered data and tack time for the module</returns>
        [HttpGet]
        [Route("api/filter/{module}/{date}/{starttime}/{endtime}")]
        public IActionResult GetDataByModulefilter(string module, DateTime date, TimeSpan starttime, TimeSpan endtime)
        {
            var latestData = _dbContext2.KreedaIotTestNew
                .Where(x => x.Module == module &&
                            x.Date == date &&
                            x.Time >= starttime &&
                            x.Time <= endtime &&
                            x.TotalRuntime > 0 &&
                            x.DeltaTime <= 70000)
                .ToList()
                 .GroupBy(x => x.ChipID)
                 .Select(userGroup => new
                 {
                     UserName = ExtractChipPart(userGroup.First().ChipID),
                     deltaAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.DeltaTime / 1000.0))).Item3),
                     runAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3)
                 })
                 .OrderBy(data => ExtractNumericPart(ExtractChipPrefix(data.UserName)))
                 .ToList();

            var tack = _dbContext1.KreedaIot_TackTime
             .Where(t => t.Module == module)
             .Select(t => t.Tack)
             .FirstOrDefault();

            var result = new Dictionary<string, object>
            {
                { module, new { latestData, tack } }
            };

            return Json(result);
        }

        /// <summary>
        /// Retrieves real-time data for a specific module
        /// </summary>
        /// <param name="module">The module identifier</param>
        /// <returns>Returns JSON containing the real-time data and tack time for the module</returns>
        [HttpGet]
        [Route("api/realtime/data/{module}")]
        public IActionResult GetDataByModuler(string module)
        {
            TimeZoneInfo sriLankanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            DateTime sriLankanTime = TimeZoneInfo.ConvertTime(DateTime.Now, sriLankanTimeZone);
            int currentHour = sriLankanTime.Hour;

            var groupedData = _dbContext.iot
                .Where(x => x.Plant == "SYNERGY" && x.Module == module && x.TotalRuntime <= 70000 && x.DeltaTime <= 60000)
                .GroupBy(x => new { x.style, x.UserName, x.Operation, x.ChipID })
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList();

            var tack = _dbContext1.KreedaIot_TackTime
                .Where(t => t.Module == module)
                .Select(t => t.Tack)
                .FirstOrDefault();

            var latestData = groupedData
                .OrderBy(x => ExtractNumericPart(ExtractChipPrefix(x.ChipID)))
                .Select(x => new
                {
                    UserName = ExtractChipPart(x.ChipID),
                    DeltaAvg = Math.Round(x.DeltaTime / 1000.0, 0),
                    RunAvg = Math.Round(x.TotalRuntime / 1000.0, 0),
                    Hand = x.Hand,
                    Sew = x.Sew
                })
                .ToList();

            var result = new Dictionary<string, object>
            {
                { module, new { latestData, tack } }
            };

            return Json(result);
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

            var mostFrequent = clusters.OrderByDescending(c => c.count).FirstOrDefault();
            return (mostFrequent.start, mostFrequent.end, mostFrequent.average);
        }

        /// <summary>
        /// Extracts the part of the chip ID before the "/" character
        /// </summary>
        /// <param name="chipID">The complete chip ID</param>
        /// <returns>Returns the extracted part of the chip ID</returns>
        private string ExtractChipPart(string chipID)
        {
            if (string.IsNullOrEmpty(chipID)) return "";
            var parts = chipID.Split('/');
            return parts.Length > 0 ? parts[0] : chipID;
        }

        /// <summary>
        /// Extracts the prefix part of the chip ID
        /// </summary>
        /// <param name="chipID">The complete chip ID</param>
        /// <returns>Returns the prefix part of the chip ID</returns>
        private string ExtractChipPrefix(string chipID)
        {
            if (string.IsNullOrEmpty(chipID)) return "";
            var parts = chipID.Split('_');
            return parts.Length > 0 ? parts[0] : chipID;
        }

        /// <summary>
        /// Extracts the numeric part from a module name
        /// </summary>
        /// <param name="moduleName">The module name to extract from</param>
        /// <returns>Returns the numeric part of the module name</returns>
        private int ExtractNumericPart(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return 0;
            var numericPart = new string(moduleName.Where(char.IsDigit).ToArray());
            return int.TryParse(numericPart, out int result) ? result : 0;
        }
    }
}
