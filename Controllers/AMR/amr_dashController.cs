using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models.AMR;


namespace Iot_dashboard.Controllers.AMR
{



    public class amr_dashController : Controller
    {
        public class AppDbContext12 : DbContext
        {
            public DbSet<park> park { get; set; }

            public AppDbContext12(DbContextOptions<AppDbContext12> options) : base(options)
            {
            }
        }
        private readonly AppDbContext12 _dbContext;


        public amr_dashController(AppDbContext12 dbContext)
        {
            _dbContext = dbContext;


        }
        [HttpGet]
        public IActionResult SearchBySlot(string slot)
        {
           
            var slotData = _dbContext.park.FirstOrDefault(p => p.Slot == slot);

            if (slotData != null)
            {
               
                return Json(new { category = slotData.Category, trolly = slotData.status });
            }

          
            return Json(new { });
        }

        [HttpPost]
        public IActionResult AddNewData([FromBody] park newData)
        {
            try
            {
                _dbContext.park.Add(newData);
                _dbContext.SaveChanges();


                return Ok("Data added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpPost]
        public IActionResult UpdateData([FromBody] park updatedData)
        {
            try
            {



                var existingData = _dbContext.park.FirstOrDefault(p => p.Slot == updatedData.Slot);

                if (existingData != null)
                {
                    existingData.Category = updatedData.Category;
                    existingData.status = updatedData.status;


                    _dbContext.SaveChanges();


                    return Ok("Data updated successfully");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            return NotFound("Data not found for the given slot");
        }



        [HttpPost]
        public IActionResult DeleteData([FromBody] DeleteRequest deleteRequest)
        {
            try
            {
                var slotToDelete = deleteRequest.Slot;

                var dataToDelete = _dbContext.park.FirstOrDefault(p => p.Slot == slotToDelete);

                if (dataToDelete != null)
                {
                    _dbContext.park.Remove(dataToDelete);
                    _dbContext.SaveChanges();
                    return Ok("Data deleted successfully");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }


            return NotFound("Data not found for the given slot");
        }

        public class DeleteRequest
        {
            public string Slot { get; set; }
        }


        [HttpGet]
        public IActionResult GetParkingData()
        {
            var parkingData = _dbContext.park.ToList(); 
            return Json(parkingData); 
        }


     
        public IActionResult Index()
        {
            return View("~/Views/AMR/AMRdash.cshtml");
        }

        [HttpGet]
        public IActionResult GetTrollyData()
        {
            try
            {
                var trollyData = _dbContext.park.Where(t => t.status != "Empty").ToList();

                return Json(trollyData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
