using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext5 : DbContext
    {
        public DbSet<KreedaIot_AMR> amr { get; set; }

        public AppDbContext5(DbContextOptions<AppDbContext5> options) : base(options)
        {
        }
    }
}
