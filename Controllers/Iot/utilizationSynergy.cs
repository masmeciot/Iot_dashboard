using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    public class UtilizationSynergy : Controller
    {
        private readonly AppDbContext1000 _dbContext;
        private readonly ILogger<UtilizationSynergy> _utilizationLogger;

        public UtilizationSynergy(AppDbContext1000 dbContext, ILogger<UtilizationSynergy> utilizationLogger)
        {
            _dbContext = dbContext;
            _utilizationLogger = utilizationLogger;
        }

        public IActionResult Index()
        {
            return View("~/Views/utilizationSynergy.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetUtilizeIotData(DateTime? date)
        {
            try
            {
                var query = _dbContext.KreedaIOTTestNew
                    .Where(x => x.Plant == "SYNERGY");

                if (date.HasValue)
                {
                    query = query.Where(x => x.Date == date.Value.Date);
                }

                var groupedResults = await query
                    .GroupBy(x => new { x.ChipID, x.UserName, x.Plant, x.Operation, x.MachineID, x.Shift, x.Qty })
                    .Select(g => new
                    {
                        ChipID = g.Key.ChipID,
                        UserName = g.Key.UserName,
                        Plant = g.Key.Plant,
                        Operation = g.Key.Operation,
                        MachineID = g.Key.MachineID,
                        Shift = g.Key.Shift,
                        TotalRuntimeSum = g.Sum(x => x.TotalRuntime),
                        TotalQty = g.Sum(x => x.Qty)
                    })
                    .OrderByDescending(x => x.UserName)
                    .Take(100)
                    .ToListAsync();

                // Get all unique operations from the results
                var operations = groupedResults.Select(x => x.Operation).Distinct().ToList();

                // Query KreedIot_UserSMV for hand and sew values using ToListAsync and then create a lookup manually
                var smvList = await _dbContext.Set<KreedIot_UserSMV>()
                    .Where(s => operations.Contains(s.Operation))
                    .ToListAsync();

                // Create a lookup from the list
                var smvLookup = smvList.ToLookup(s => s.Operation ?? string.Empty);

                // Map hand and sew values to each result using the lookup
                var resultWithSmv = groupedResults.Select(x =>
                {
                    var smvGroup = smvLookup[x.Operation ?? string.Empty];
                    var smv = smvGroup.FirstOrDefault();
                    return new
                    {
                        x.ChipID,
                        x.UserName,
                        x.Plant,
                        x.Operation,
                        x.MachineID,
                        x.Shift,
                        x.TotalRuntimeSum,
                        x.TotalQty,
                        Hand = smv?.Hand ?? 1,
                        Sew = smv?.sew ?? 1
                    };
                });

                return Json(resultWithSmv);
            }
            catch (Exception ex)
            {
                _utilizationLogger.LogError(ex, "An error occurred while fetching KreedaIOTTestNew data.");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class AppDbContext1001 : DbContext
    {
        public DbSet<KreedaIOTTestNew> KreedaIOTTestNew { get; set; }
        public DbSet<KreedIot_UserSMV> KreedIot_UserSMV { get; set; } // Add this line

        public AppDbContext1001(DbContextOptions<AppDbContext1001> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KreedaIOTTestNew>()
                .HasKey(k => k.ID);

            modelBuilder.Entity<KreedIot_UserSMV>() // Add this configuration if needed
                .HasKey(k => k.ID); // Replace 'ID' with the actual primary key property of KreedIot_UserSMV
        }
    }
}