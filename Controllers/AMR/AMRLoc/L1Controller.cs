using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Iot_dashboard.Hubs;
using Iot_dashboard.Models.AMR;
using static Iot_dashboard.Controllers.AMR.confirmController;
using static Iot_dashboard.Controllers.AMR.amr_dashController;
using static Iot_dashboard.Controllers.AMR.L1Controller;

namespace Iot_dashboard.Controllers.AMR
{


    public class L1Controller : Controller
    {


        public class AppDbContext6 : DbContext
        {
            public DbSet<KreedaIot_AMR> amr { get; set; }

            public AppDbContext6(DbContextOptions<AppDbContext6> options) : base(options)
            {
            }
        }
        private readonly AppDbContext6 _dbContext;

        private readonly AppDbContext17 _dbContext1;

        public class AppDbContext17 : DbContext
        {
            public DbSet<park> park { get; set; }

            public AppDbContext17(DbContextOptions<AppDbContext17> options) : base(options)
            {
            }
        }




        public L1Controller(AppDbContext6 dbContext, AppDbContext17 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }


        public IActionResult Index()
        {
            return View("~/Views/AMR/AMR_loc/L1.cshtml");
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
                   .Where(entry => entry.location == "L1" && entry.status != "Complete")
                    .OrderByDescending(entry => entry.ID)

                    .Select(entry => new Data
                    {
                        //module = entry.Module,
                        tRNO = entry.TRNO,
                        Location = "",
                        time = entry.Time.ToString(@"hh\:mm\:ss"),
                        date=entry.Date.ToString("yyyy-MM-dd"),
                        Status = entry.status,
                        Id = entry.ID,
                        job=entry.JobID,
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


        [HttpPost]
        public IActionResult UpdateData([FromBody] park updatedData)
        {

            var existingData = _dbContext.amr.FirstOrDefault(p => p.ID == updatedData.ID);

            if (existingData != null)
            {

                existingData.status = "Complete";

                _dbContext.SaveChanges();


                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given slot");
        }

    }
}


