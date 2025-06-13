using Iot_dashboard.Models;
using Iot_dashboard.Models.AMR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Iot_dashboard.Hubs
{
    public class StatusHub : Hub
    {

        private readonly AppDbContext5 _dbContext;

        public StatusHub(AppDbContext5 dbContext)
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
                var item = await _dbContext.amr.FirstOrDefaultAsync(a => a.ID == id);

                if (item != null)
                {
                    // Check for null value in 'status' property before accessing it
                    if (item.status != null)
                    {
                        item.status = "confirm";
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine("nullll");
                        // Handle the case where 'status' is null
                    }
                }
                else
                {
                    Console.WriteLine("ID not found");
                    // Handle the case where the item with the specified ID isn't found
                }
            }
            catch (Exception ex)
            {
                // Log and handle the exception
                Console.WriteLine("Error processing data: " + ex.Message);
            }
        }



    }
}
