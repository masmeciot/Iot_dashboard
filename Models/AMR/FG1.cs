using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext23 : DbContext
    {
        public DbSet<KreedaIot_AMR> amr { get; set; }

        public AppDbContext23(DbContextOptions<AppDbContext23> options) : base(options)
        {
        }
    }
}
