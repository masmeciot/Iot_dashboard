using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext10 : DbContext
    {
        public DbSet<KreedaIot_AMR> amr { get; set; }

        public AppDbContext10(DbContextOptions<AppDbContext10> options) : base(options)
        {
        }
    }
}
