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
                var result = Enumerable.Empty<object>();

                if (date.HasValue)
                {
                    result = await _dbContext.KreedaIOTTestNew
                        .Where(x => x.Date == date.Value.Date && x.Plant == "SYNERGY") // ✅ Filter for SYNERGY
                        // .Where(x => x.Date == date.Value.Date)
                        .GroupBy(x => new { x.ChipID, x.UserName, x.Plant, x.Operation, x.MachineID, x.Shift })
                        .Select(g => new
                        {
                            ChipID = g.Key.ChipID,
                            UserName = g.Key.UserName,
                            Plant = g.Key.Plant,
                            Operation = g.Key.Operation,
                            MachineID = g.Key.MachineID,
                            Shift = g.Key.Shift,
                            TotalRuntimeSum = g.Sum(x => x.DeltaTime)
                        })
                        .OrderByDescending(x => x.UserName)
                        .Take(100)
                        .ToListAsync();
                }
                else
                {
                    result = await _dbContext.KreedaIOTTestNew
                        .Where(x => x.Plant == "SYNERGY") //✅ Filter for SYNERGY
                        .OrderByDescending(x => x.Date)
                        .Take(100)
                        .ToListAsync();
                }

                return Json(result);
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

        public AppDbContext1001(DbContextOptions<AppDbContext1001> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KreedaIOTTestNew>()
                .HasKey(k => k.ID);
        }
    }
}