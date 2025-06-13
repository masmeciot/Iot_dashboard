using Iot_dashboard.Hubs;
using Iot_dashboard.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Iot_dashboard.Controllers.andon
{
    public class andonCompController : Controller
    {
        public class AppDbContext15 : DbContext
        {
            public DbSet<KreedaIot_Andon_raised> table { get; set; }

            public AppDbContext15(DbContextOptions<AppDbContext15> options) : base(options)
            {
            }
        }
        private readonly AppDbContext15 _dbContext;


        public andonCompController(AppDbContext15 dbContext)
        {
            _dbContext = dbContext;

        }
        public IActionResult Index()
        {
            return View("~/Views/andon/andonCompl.cshtml");
        }




        [HttpGet]
        public async Task<IActionResult> GetPendingAndonDataForToday()
        {
            try
            {
                DateTime today = DateTime.Today;

                var pendingAndonData = await _dbContext.table
                    .Where(entry => entry.date_raised.Date == today && entry.status == "pending")
                    .Select(entry => new
                    {
                        entry.ID,
                        entry.machine_id,
                        entry.user_raised_by,
                        entry.module,
                        entry.andon_category,
                        entry.andon_issue,
                        DateRaised = entry.date_raised.ToString("yyyy-MM-dd"),

                        // Add 5 hours and 30 minutes to the time from the table
                        StartTime = (entry.andon_start_time + TimeSpan.FromHours(5) + TimeSpan.FromMinutes(30)).ToString("HH:mm:ss"),


                    })
                    .ToListAsync();

                return Ok(pendingAndonData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }



    }
}