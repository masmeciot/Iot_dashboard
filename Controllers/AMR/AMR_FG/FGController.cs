using Microsoft.AspNetCore.Mvc;
using Iot_dashboard.Models.AMR;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.AMR.AMR_FG
{
    public class FGController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/AMR/AMR_FG/FG.cshtml");
        }


        private readonly AppDbContext27 _dbContext;
        private readonly AppDbContext28 _dbContext1;
        public class AppDbContext27 : DbContext
        {
            public DbSet<KreedaIot_AMR> amr { get; set; }

            public AppDbContext27(DbContextOptions<AppDbContext27> options) : base(options)
            {
            }
        }


        public class AppDbContext28 : DbContext
        {
            public DbSet<park> park { get; set; }

            public AppDbContext28(DbContextOptions<AppDbContext28> options) : base(options)
            {
            }
        }



        public FGController(AppDbContext27 dbContext, AppDbContext28 dbContext1)
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


        [HttpPost]
        public IActionResult UpdateData([FromBody] park updatedData)
        {
            try { 
            DateTime today = DateTime.Today;
            var existingData = _dbContext.amr.FirstOrDefault(p => p.ID == updatedData.ID);
            DateTime sriLankanTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"));

            if (existingData != null)
            {
                existingData.Next = "L3O";
                existingData.Date = today;
                existingData.Time = sriLankanTime.TimeOfDay;
                _dbContext.SaveChanges();

                existingData.status = "Complete";
                _dbContext.SaveChanges();

        
                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given slot");
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException)
            {
                // Database connection or transient error
                return StatusCode(503, "Database unavailable. Please try again later.");
            }
            catch (Exception ex)
            {
                // Other unhandled exceptions
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {


                var Data = await _dbContext.amr
                   .Where(entry => entry.location == "FG" && entry.status != "Complete")
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
