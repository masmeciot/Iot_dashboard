using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;


namespace Iot_dashboard.Controllers.andon
{
    public class andonreportController : Controller
    {

        public class AppDbContext20 : DbContext
        {
            public DbSet<KreedaIot_Andon_raised> table { get; set; }

            public AppDbContext20(DbContextOptions<AppDbContext20> options) : base(options)
            {
            }
        }
        private readonly AppDbContext20 _dbContext;


        public andonreportController(AppDbContext20 dbContext)
        {
            _dbContext = dbContext;
          


        }
        [HttpPost]
        public async Task<IActionResult> CompletedTdy(DateTime selectedDate)
        {
            try
            {
                var filteredData = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == selectedDate.Date)
                    .Select(entry => new
                    {
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        entry.resolved_by,
                        entry.status,
                        TimeGapMinutes = (entry.andon_resolved_time - entry.andon_start_time).TotalMinutes
                    })
                    .ToListAsync();

                return Json(filteredData); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AllData()
        {
            try
            {
                var allData = await _dbContext.table
                    .Select(entry => new
                    {
                        entry.ID,
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        entry.resolved_by,
                        entry.status,
                        entry.andon_start_time,
                        entry.andon_resolved_time,

                        TimeGapMinutes = (entry.andon_resolved_time - entry.andon_start_time).TotalMinutes
                    })
                    .ToListAsync();

                // Create CSV file
                using (var memoryStream = new MemoryStream())
                using (var writer = new StreamWriter(memoryStream))
                using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(allData);
                    writer.Flush();
                    memoryStream.Position = 0;

                    // Return CSV file as download
                    return File(memoryStream.ToArray(), "text/csv", "andon_data.csv");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // Log the exception details
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }




        public IActionResult Index()
        {
            return View("~/Views/andon/andonReport.cshtml");
        }
    }
}
