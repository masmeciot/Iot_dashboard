using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Iot_dashboard.Hubs;
using Iot_dashboard.Models.AMR;

namespace Iot_dashboard.Controllers.AMR
{


    public class orderController : Controller
    {

        private readonly AppDbContext14 _dbContext;

        public class AppDbContext14 : DbContext
        {
            public DbSet<order> order { get; set; }

            public AppDbContext14(DbContextOptions<AppDbContext14> options) : base(options)
            {
            }
        }


      


        public orderController(AppDbContext14 dbContext, AppDbContext14 dbContext1)
        {
            _dbContext = dbContext;
           
        }



        public IActionResult Index()
        {
            return View("~/Views/AMR/amr_order.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            try
            {
                var allData = await _dbContext.order
                    .OrderByDescending(entry => entry.ID)
                    .Select(entry => new
                    {
                        ID = entry.ID,
                        orderID = entry.orderID,
                        status = entry.status,
                        fail = entry.fail,
                        vehicle = entry.vehicle,
                        start = entry.start,
                        endt = entry.endt,
                        compt = entry.compt,
                        
                    })
                    .ToListAsync();

                return Json(allData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all data: {ex.Message}");
                return StatusCode(500, new { ErrorMessage = "Internal Server Error", ExceptionMessage = ex.Message });
            }
        }











    }
}


