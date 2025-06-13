using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Models.AMR
{
    public class AppDbContext9 : DbContext
    {
        public DbSet<KreedaIot_AMR> amr { get; set; }

        public AppDbContext9(DbContextOptions<AppDbContext9> options) : base(options)
        {
        }
    }
}
