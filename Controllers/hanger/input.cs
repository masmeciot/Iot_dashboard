using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.hanger
{
    public class input : Controller
    {
        public class AppDbContext78 : DbContext
        {
            public DbSet<hanger_incentive> hanger_incentive { get; set; }

            public AppDbContext78(DbContextOptions<AppDbContext78> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<hanger_incentive>()
                    .HasKey(h => h.date); // Set date as the primary key
            }
        }

        public class AppDbContext79 : DbContext
        {
            public DbSet<hanger_output> hanger_output { get; set; }

            public AppDbContext79(DbContextOptions<AppDbContext79> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<hanger_output>()
                    .HasKey(h => h.date); // Set date as the primary key
            }
        }

        public input(AppDbContext78 dbContext, AppDbContext79 dbcontext1)
        {
            _dbContext = dbContext;
            _dbContext1 = dbcontext1;
        }

        private readonly AppDbContext78 _dbContext;
        private readonly AppDbContext79 _dbContext1;
        public IActionResult Index()
        {
            return View("~/Views/hanger/DataEnput.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(
      DateOnly dayOutputDate, int dayOutputQuantity, string dayOutputSoli, string style, string module,
      DateOnly incentiveDate, int incentiveQuantity, string incentiveSoli, string module1)
        {
            // Save or update hanger_output
            var existingOutput = await _dbContext1.hanger_output
                .FirstOrDefaultAsync(h => h.date == dayOutputDate && h.soli == dayOutputSoli);
            if (existingOutput != null)
            {
                existingOutput.amount = dayOutputQuantity; // Update existing record
                existingOutput.Style = style; // Update style
                existingOutput.Module = module; // Update module
                _dbContext1.hanger_output.Update(existingOutput);
            }
            else
            {
                _dbContext1.hanger_output.Add(new hanger_output
                {
                    date = dayOutputDate,
                    amount = dayOutputQuantity,
                    soli = dayOutputSoli,
                    Style = style,
                    Module = module
                });
            }

            // Save or update hanger_incentive
            var existingIncentive = await _dbContext.hanger_incentive
                .FirstOrDefaultAsync(h => h.date == incentiveDate && h.soli == incentiveSoli);
            if (existingIncentive != null)
            {
                existingIncentive.amount = incentiveQuantity; // Update existing record
                existingIncentive.Module = module1; // Update module
                _dbContext.hanger_incentive.Update(existingIncentive);
            }
            else
            {
                _dbContext.hanger_incentive.Add(new hanger_incentive
                {
                    date = incentiveDate,
                    amount = incentiveQuantity,
                    soli = incentiveSoli,
                    Module = module1
                });
            }

            // Save changes to both databases
            await _dbContext.SaveChangesAsync();
            await _dbContext1.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}