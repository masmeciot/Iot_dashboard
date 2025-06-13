using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    public class outAnalyze : Controller
    {

        public class AppDbContext58 : DbContext
        {
            public DbSet<IotOut> IotOUT { get; set; }

            public AppDbContext58(DbContextOptions<AppDbContext58> options) : base(options)
            {
            }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<IotOut>().HasNoKey();
            }
        }


        private readonly AppDbContext58 _dbContext;

        public outAnalyze(AppDbContext58 dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetModuleData()
        {
            var moduleData = await _dbContext.IotOUT
                                              .GroupBy(i => new { i.Module, i.UserName, i.style })
                                              .Select(g => new
                                              {
                                                  Module = g.Key.Module,
                                                  UserName = g.Key.UserName,
                                                  SumOfH = g.Sum(i => i.H1 + i.H2 + i.H3 + i.H4 + i.H5 + i.H6 + i.H7 + i.H8 + i.H9)
                                              })
                                              .ToListAsync();

            return Json(moduleData);
        }



        public IActionResult Index()
        {
            return View("~/Views/Home/outanalyze.cshtml");
        }

    }
}

