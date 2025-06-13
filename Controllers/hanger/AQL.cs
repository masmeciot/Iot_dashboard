using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Iot_dashboard.Controllers.hanger
{
    public class AQL : Controller
    {

        public class AppDbContext77 : DbContext
        {
            public DbSet<hanger_aql> hanger_aql { get; set; }

            public AppDbContext77(DbContextOptions<AppDbContext77> options) : base(options)
            {

            }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<hanger_aql>()
                    .HasKey(h => h.id);  // Explicitly set the primary key
            }

        }

        public AQL(AppDbContext77 dbContext)
        {
            _dbContext = dbContext;
        }



        [HttpPost]
        public IActionResult UpdateCount([FromBody] int change)
        {
            var today = DateTime.UtcNow.Date;  // Use UTC to avoid time zone issues
            var existingEntry = _dbContext.hanger_aql.FirstOrDefault(e => e.date == today);

            if (existingEntry != null)
            {
                existingEntry.count += change;
                _dbContext.Update(existingEntry);
            }
            else
            {
                var newEntry = new hanger_aql
                {
                    date = today,
                    count = change
                };
                _dbContext.Add(newEntry);
            }

            _dbContext.SaveChanges();

            // Return the updated count
            var updatedCount = _dbContext.hanger_aql.FirstOrDefault(e => e.date == today)?.count ?? change;
            return Json(new { success = true, newCount = updatedCount });
        }


        private readonly AppDbContext77 _dbContext;
        public IActionResult Index()
        {
            return View("~/Views/hanger/AQL.cshtml");
        }
    }
}
