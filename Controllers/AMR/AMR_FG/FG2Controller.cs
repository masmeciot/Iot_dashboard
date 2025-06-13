using Microsoft.AspNetCore.Mvc;
using Iot_dashboard.Models.AMR;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.AMR.AMR_FG
{
    public class FG2Controller : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AMR/AMR_FG/FG222.cshtml");
        }


        private readonly AppDbContext25 _dbContext;
        private readonly AppDbContext26 _dbContext1;
        public class AppDbContext25 : DbContext
        {
            public DbSet<KreedaIot_AMR> amr { get; set; }

            public AppDbContext25(DbContextOptions<AppDbContext25> options) : base(options)
            {
            }
        }


        public class AppDbContext26 : DbContext
        {
            public DbSet<park> park { get; set; }

            public AppDbContext26(DbContextOptions<AppDbContext26> options) : base(options)
            {
            }
        }



        public FG2Controller(AppDbContext25 dbContext, AppDbContext26 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }

        public class Data
        {
            public string date { get; set; }
            public string tRNO { get; set; }
            public string time { get; set; }
            public string job { get; set; }
            public string Status { get; set; }
            public int Id { get; set; }
            public string Location { get; set; }
        }



        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {


                var Data = await _dbContext.amr
                   .Where(entry => entry.location == "FG2" && entry.status != "Complete")
                    .OrderByDescending(entry => entry.ID)

                    .Select(entry => new Data
                    {
                        //module = entry.Module,
                        tRNO = entry.TRNO,
                        Location = "",
                        time = entry.Time.ToString(@"hh\:mm\:ss"),
                        date = entry.Date.ToString("yyyy-MM-dd"),
                        Status = entry.status,
                        Id = entry.ID,
                        job = entry.JobID,
                    })
                    .ToListAsync();
                foreach (var entry in Data)
                {
                    var location = await _dbContext1.park
                        .Where(p => p.status == entry.tRNO)
                        .Select(p => p.Slot)
                        .FirstOrDefaultAsync();

                    entry.Location = location;
                }

                return Json(Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }

    }
}
