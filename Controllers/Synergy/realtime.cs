using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Synergy
{
    /// <summary>
    /// Controller responsible for handling real-time data operations for Synergy devices
    /// </summary>
    public class realtime : Controller
    {
        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext51 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext51(DbContextOptions<AppDbContext51> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey();
            }
        }
        private readonly AppDbContext51 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT dashboard data</param>
        public realtime(AppDbContext51 dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves and processes real-time data for all Synergy devices
        /// </summary>
        /// <returns>Returns JSON containing processed real-time data for all modules</returns>
        [HttpGet]
        public IActionResult Data()
        {
            // Fetch data for the specific condition
            var filteredData = _dbContext.iot
                .Where(x => x.Plant == "SYNERGY" && x.TotalRuntime <= 70000 && x.DeltaTime <= 60000)
                .ToList();

            // Group by module, then find the most recent style for each module
            var mostRecentStyleData = filteredData
                .GroupBy(x => x.Module) // Group by Module
                .SelectMany(moduleGroup =>
                {
                    // Within each module, find the most recent style
                    var mostRecentStyle = moduleGroup
                        .OrderByDescending(x => x.Time) // Assuming `Time` indicates recency
                        .FirstOrDefault()?.style;

                    // Return only rows matching the most recent style for this module
                    return moduleGroup.Where(x => x.style == mostRecentStyle);
                })
                .ToList();

            // Perform grouping and transformations
            var groupedData = mostRecentStyleData
                .GroupBy(x => new { x.style, x.UserName, x.Operation })
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList();

            var result = groupedData
                .GroupBy(x => x.Module)
                .OrderBy(g => ExtractNumericPart(g.Key))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new
                    {
                        UserName = ExtractChipPart(x.ChipID),
                        DeltaAvg = Math.Round(x.DeltaTime / 1000.0, 0),
                        RunAvg = Math.Round(x.TotalRuntime / 1000.0, 0),
                        Hand = x.Hand,
                        Sew = x.Sew
                    })
                    .OrderBy(user => GetCustomOrder(user.UserName)) // Custom order for userName
                    .ToArray()
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
            return parts.Length > 0 ? parts[0] : string.Empty; // Return the prefix (e.g., M01)
        }

        /// <summary>
        /// Displays the real-time data interface
        /// </summary>
        /// <returns>Returns the real-time data view</returns>
        public IActionResult Index()
        {
            return View("~/Views/synergy/realtime.cshtml");
        }
    }
}
