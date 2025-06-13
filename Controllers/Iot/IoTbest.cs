using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System.Reflection;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling IoT best performance data and statistics
    /// </summary>
    public class IoTbestController : Controller
    {
        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext35 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext35(DbContextOptions<AppDbContext35> options) : base(options)
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
        public class AppDbContext36 : DbContext
        {
            public DbSet<tack> KreedaIot_TackTime { get; set; }

            public AppDbContext36(DbContextOptions<AppDbContext36> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }

        private readonly AppDbContext35 _dbContext;
        private readonly AppDbContext36 _dbContext1;

        /// <summary>
        /// Constructor that initializes the database contexts
        /// </summary>
        /// <param name="dbContext">The database context for IoT dashboard data</param>
        /// <param name="dbContext1">The database context for tack time data</param>
        public IoTbestController(AppDbContext35 dbContext, AppDbContext36 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }

        /// <summary>
        /// Retrieves and processes the best performance data for all modules
        /// </summary>
        /// <returns>Returns JSON containing grouped performance data by module and user</returns>
        [HttpGet]
        public IActionResult Data()
        {
            // Get the latest data for each style, username, and operation combination
            var groupedData = _dbContext.iot
                .Where(x => x.Plant == "MEC" && x.DeltaTime <= 70000)
                .GroupBy(x => new { x.style, x.UserName, x.Operation })
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList();

            // Process and group the data by module and user
            var result = groupedData
                .GroupBy(x => x.Module)
                .OrderBy(g => ExtractNumericPart(g.Key)) 
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.UserName) 
                          .Select(userGroup => new
                          {
                              UserName = userGroup.Key,
                              Hand = userGroup.First().Hand,
                              Sew = userGroup.First().Sew,
                              MachineID = userGroup.First().MachineID,
                              // Calculate the most frequent Delta and Run values
                              deltaAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.DeltaTime / 1000.0))).Item3),
                              runAvg = Math.Round(GetMostFrequentRange(userGroup.Select(x => (int)Math.Round(x.TotalRuntime / 1000.0))).Item3)
                          })
                          .ToArray()
                );

            return Json(result);
        }

        /// <summary>
        /// Extracts the numeric part from a module name for sorting purposes
        /// </summary>
        /// <param name="moduleName">The module name to extract the numeric part from</param>
        /// <returns>Returns the numeric part as an integer, or 0 if no numeric part is found</returns>
        private int ExtractNumericPart(string moduleName)
        {
            var numericPart = new string(moduleName.Where(char.IsDigit).ToArray());
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
        /// Displays the IoT best performance interface
        /// </summary>
        /// <returns>Returns the IoT best performance view</returns>
        public IActionResult Index()
        {
            return View("~/Views/IoTbest.cshtml");
        }
    }
}
