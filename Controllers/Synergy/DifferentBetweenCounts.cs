using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Synergy
{
    public class DifferentBetweenCounts : Controller
    {
        public class AppDbContext67 : DbContext
        {
            public DbSet<KreedaIotTestNew> KreedaIotTestNew { get; set; }

            public AppDbContext67(DbContextOptions<AppDbContext67> options) : base(options) { }
        }

        private readonly AppDbContext67 _dbContext;

        public DifferentBetweenCounts(AppDbContext67 dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> GetChipIdDifferences([FromBody] FilterModel filter)
        {
            // Filter data and limit to 10000 rows
            var data = await _dbContext.KreedaIotTestNew
                .Where(d => d.style == filter.Style && d.Module == filter.Module)
                .Take(10000)
                .ToListAsync();

            // Extract ChipID prefixes and count occurrences
            var chipCounts = data
                .GroupBy(d => d.ChipID.Split('/')[0]) // Extract prefix (e.g., M01)
                .Select(g => new { Prefix = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Prefix, x => x.Count);

            // Get M28 count and calculate differences
            int m28Count = chipCounts.ContainsKey("M28") ? chipCounts["M28"] : 0;
            var differences = chipCounts
                .Where(kvp => kvp.Key != "M28") // Exclude M28 itself
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value - m28Count);

            // Prepare response
            var response = new
            {
                fgcount = m28Count, // M28 count
                differences // Differences for each chip ID
            };

            return Json(response);
        }

        public class FilterModel
        {
            public string Style { get; set; }
            public string Module { get; set; }
        }
        public IActionResult Index()
        {
            return View("~/Views/synergy/DifferentBetweenCount.cshtml");
        }
    }
}
