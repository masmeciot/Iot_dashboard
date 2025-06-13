using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext30 : DbContext
    {
       public DbSet<park> park { get; set; }

        public AppDbContext30(DbContextOptions<AppDbContext30> options) : base(options)
        {
        }
    }
}
