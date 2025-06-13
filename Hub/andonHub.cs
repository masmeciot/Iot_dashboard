using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Iot_dashboard.Hubs
{
    public class andonHub : Hub
    {
        public async Task SendEfficiencyUpdate(decimal efficiencyPercentage)
        {
            Console.WriteLine($"Received update: {efficiencyPercentage}");
            await Clients.All.SendAsync("ReceiveEfficiencyUpdate", efficiencyPercentage);
        }
        public async Task SendEfficiencyByDateLastSevenDaysUpdate(object groupedData)
        {
            Console.WriteLine("Received efficiency data for the last seven days:");
            foreach (var entry in (IEnumerable<dynamic>)groupedData)
            {
                Console.WriteLine($"Date: {entry.Date}, Average Efficiency: {entry.AverageEfficiency}");
            }
            await Clients.All.SendAsync("ReceiveEfficiencyByDateLastSevenDays", groupedData);
        }
    }
}
