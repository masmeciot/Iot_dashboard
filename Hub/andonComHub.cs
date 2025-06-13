using Iot_dashboard.Models;
using Iot_dashboard.Models.AMR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using System.Threading.Tasks;

namespace Iot_dashboard.Hubs
{
    public class andonComHub : Hub
    {
        public class AppDbContext16 : DbContext
        {
            public DbSet<KreedaIot_Andon_raised> table { get; set; }

            public AppDbContext16(DbContextOptions<AppDbContext16> options) : base(options)
            {
            }
        }

        private readonly AppDbContext16 _dbContext;

        public andonComHub(AppDbContext16 dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task TriggerUpdate(string idString)
        {
            if (int.TryParse(idString, out int id))
            {
                Console.WriteLine($"Received ID: {id}");
                // Perform the specific task related to the received ID
                await PerformTaskForId(id);
            }
            else
            {

            }
        }

        private async Task PerformTaskForId(int id)
        {
            try
            {
                var item = await _dbContext.table.FirstOrDefaultAsync(a => a.ID == id);

          
                        item.status = "completed";



                item.andon_resolved_time = DateTime.UtcNow;



                item.resolved_by = "Rusith"; 
                        

                        await _dbContext.SaveChangesAsync();
                  

            }
            catch (Exception ex)
            {
                // Log and handle other exceptions
                Console.WriteLine("Error processing data: " + ex.Message);
            }
        }






    }
}
