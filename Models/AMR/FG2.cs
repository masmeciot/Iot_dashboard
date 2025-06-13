using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext24 : DbContext
    {
        public DbSet<KreedaIot_AMR> amr { get; set; }

        public AppDbContext24(DbContextOptions<AppDbContext24> options) : base(options)
        {
        }
    }
}
