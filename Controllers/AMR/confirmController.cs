using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Iot_dashboard.Hubs;
using Iot_dashboard.Models.AMR;

namespace Iot_dashboard.Controllers.AMR
{


    public class confirmController : Controller
    {

        private readonly AppDbContext4 _dbContext;
        private readonly AppDbContext13 _dbContext1;
        public class AppDbContext4 : DbContext
        {
            public DbSet<KreedaIot_AMR> amr { get; set; }

            public AppDbContext4(DbContextOptions<AppDbContext4> options) : base(options)
            {
            }
        }
 

        public class AppDbContext13 : DbContext
        {
            public DbSet<park> park{ get; set; }

            public AppDbContext13(DbContextOptions<AppDbContext13> options) : base(options)
            {
            }
        }
      


        public confirmController(AppDbContext4 dbContext, AppDbContext13 dbContext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
        }



        public IActionResult Index()
        {
            return View("~/Views/AMR/confirm.cshtml");
        }




        public class TopData
        {
            public string Date { get; set; }
            public string TRNO { get; set; }
            public string Time { get; set; }
            public string Next { get; set; }
            public string Status { get; set; }
            public int Id { get; set; }
            public string Location { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var top10Data = await _dbContext.amr
                    .Where(entry => entry.status == "Pending")
                    .OrderByDescending(entry => entry.ID)
                    .Select(entry => new TopData
                    {
                        Date = entry.Date.ToString("yyyy-MM-dd"),
                        TRNO = entry.TRNO,
                        Time = entry.Time.ToString(@"hh\:mm\:ss"),
                        Next = entry.Next,
                        Status = entry.status,
                        Id = entry.ID,
                        Location = "" 
                    })
                    .ToListAsync();

                foreach (var entry in top10Data)
                {
                    var location = await _dbContext1.park
                        .Where(p => p.status == entry.TRNO)
                        .Select(p => p.Slot)
                        .FirstOrDefaultAsync();

                    entry.Location = location;
                }

                return Json(top10Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Eror", ExceptionMessage = ex.Message });
            }
        }



        [HttpPost]
        public IActionResult UpdateData([FromBody] park updatedData)
        {

            var existingData = _dbContext.amr.FirstOrDefault(p => p.ID == updatedData.ID);

            if (existingData != null)
            {


                existingData.status = "confirm";

                _dbContext.SaveChanges();


                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given slot");
        }









    }
}


