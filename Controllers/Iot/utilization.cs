using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    public class utilization : Controller
    {
        private readonly AppDbContext1000 _dbContext;
        private readonly ILogger<utilization> _utilizationLogger;

        public utilization(AppDbContext1000 dbContext, ILogger<utilization> utilizationLogger)
        {
            _dbContext = dbContext;
            _utilizationLogger = utilizationLogger;
        }

        public IActionResult Index()
        {
            return View("~/Views/utilization.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetUtilizeIotData(DateTime? date)
        {
            try
            {
                dynamic result = "";
               //var query = _dbContext.KreedaIOTTestNew.AsQueryable();
               // Console.WriteLine("*************\n\n\n\n\n" + "The value of query is : \n", query );


                if (date.HasValue)
                {
                    Console.WriteLine("======\n the date is : " + date.Value.Date);



                    //query = query.Where(x => x.Date == date.Value.Date);
                    //Console.WriteLine("======\n the query inside if block is : " + query);
                    result = await _dbContext.KreedaIOTTestNew
                        .Where(x => x.Date == date.Value.Date)
                        .GroupBy(x => new { x.ChipID, x.UserName, x.Plant, x.Operation, x.MachineID, x.Shift})
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

                    Console.WriteLine("======\n the result is : " + result);
                }
                else
                {
                    Console.WriteLine("======\n the date is null");
                    //query = query.Where(x => x.Date == DateTime.Today);
                    //Console.WriteLine("======\n the query inside else block is : " + query);



                     result = await _dbContext.KreedaIOTTestNew
                                    .OrderByDescending(x => x.Date) // Ensure 'Date' exists in model
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

    public class AppDbContext1000 : DbContext
    {
        public DbSet<KreedaIOTTestNew> KreedaIOTTestNew { get; set; }

        public AppDbContext1000(DbContextOptions<AppDbContext1000> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KreedaIOTTestNew>()
                .HasKey(k => k.ID);
        }

    }
}