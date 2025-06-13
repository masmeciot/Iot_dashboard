using Iot_dashboard.Models;
using Iot_dashboard.Models.AMR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for managing machine details, user SMV data, and style information
    /// </summary>
    public class MachineController : Controller
    {
        /// <summary>
        /// Database context for managing user SMV data
        /// </summary>
        public class AppDbContext31 : DbContext
        {
            public DbSet<UserSMV> user { get; set; }

            public AppDbContext31(DbContextOptions<AppDbContext31> options) : base(options)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }

        /// <summary>
        /// Database context for managing style details
        /// </summary>
        public class AppDbContext41 : DbContext
        {
            public DbSet<style> KreedaIoT_styleDetails { get; set; }

            public AppDbContext41(DbContextOptions<AppDbContext41> options) : base(options)
            {
            }
        }

        /// <summary>
        /// Database context for managing tack time data
        /// </summary>
        public class AppDbContext32 : DbContext
        {
            public DbSet<tack> tack { get; set; }

            public AppDbContext32(DbContextOptions<AppDbContext32> options) : base(options)
            {
            }
        }

        private readonly AppDbContext31 _dbContext;
        private readonly AppDbContext32 _dbContext1;
        private readonly AppDbContext41 _dbContext2;

        /// <summary>
        /// Constructor that initializes the database contexts
        /// </summary>
        /// <param name="dbContext">Database context for user SMV data</param>
        /// <param name="dbContext1">Database context for tack time data</param>
        /// <param name="dbContext2">Database context for style details</param>
        public MachineController(AppDbContext31 dbContext, AppDbContext32 dbContext1, AppDbContext41 dbContext2)
        {
            _dbContext = dbContext;
            _dbContext1 = dbContext1;
            _dbContext2 = dbContext2;
        }

        /// <summary>
        /// Searches for slot data by user, style, operation, and plant
        /// </summary>
        /// <param name="user">The username to search for</param>
        /// <param name="style">The style to search for</param>
        /// <param name="operation">The operation to search for</param>
        /// <param name="plant">The plant to search for</param>
        /// <returns>Returns JSON containing hand and sew values for the slot</returns>
        [HttpGet]
        public IActionResult SearchBySlot(string user, string style, string operation, string plant)
        {
            var slotData = _dbContext.user.FirstOrDefault(p => p.UserName == user && p.style == style && p.Operation == operation && p.Plant == plant);

            if (slotData != null)
            {
                return Json(new { hand = slotData.Hand, sew = slotData.sew, style = slotData.style, module = slotData.Module });
            }

            return Json(new { });
        }

        /// <summary>
        /// Checks if a username exists for a given style and operation
        /// </summary>
        /// <param name="model">The model containing username, style, and operation to check</param>
        /// <returns>Returns whether the username exists</returns>
        [HttpPost]
        public IActionResult CheckUsernameExists([FromBody] UsernameCheckModel model)
        {
            bool exists = _dbContext.user.Any(u => u.UserName == model.Username && u.style == model.style && u.Operation == model.Operation);
            return Ok(new { exists });
        }

        /// <summary>
        /// Model for checking username existence
        /// </summary>
        public class UsernameCheckModel
        {
            /// <summary>
            /// The username to check
            /// </summary>
            public string Username { get; set; }

            /// <summary>
            /// The operation to check
            /// </summary>
            public string Operation { get; set; }

            /// <summary>
            /// The style to check
            /// </summary>
            public string style { get; set; }
        }

        /// <summary>
        /// Retrieves all usernames from the database
        /// </summary>
        /// <returns>Returns JSON array of usernames</returns>
        [HttpGet]
        public IActionResult GetUsernames()
        {
            var usernames = _dbContext.user.Select(u => u.UserName).ToList();
            return Json(usernames);
        }

        /// <summary>
        /// Adds new user SMV data to the database
        /// </summary>
        /// <param name="newData">The new user SMV data to add</param>
        /// <returns>Returns success message if data is added successfully</returns>
        [HttpPost]
        public IActionResult AddNewData([FromBody] UserSMV newData)
        {
            if (newData == null || string.IsNullOrWhiteSpace(newData.UserName) ||
                string.IsNullOrWhiteSpace(newData.style) || string.IsNullOrWhiteSpace(newData.Operation))
            {
                return BadRequest("Invalid data provided.");
            }

            if (_dbContext.user.Any(u => u.UserName == newData.UserName && u.style == newData.style && u.Operation == newData.Operation))
            {
                return BadRequest("Username, Style, and Operation already exist!");
            }

            _dbContext.user.Add(newData);
            _dbContext.SaveChanges();
            return Ok("Data added successfully");
        }

        /// <summary>
        /// Updates existing user SMV data in the database
        /// </summary>
        /// <param name="updatedData">The updated user SMV data</param>
        /// <returns>Returns success message if data is updated successfully</returns>
        [HttpPost]
        public IActionResult UpdateData([FromBody] UserSMV updatedData)
        {
            if (updatedData == null || string.IsNullOrWhiteSpace(updatedData.UserName) ||
                string.IsNullOrWhiteSpace(updatedData.style) || string.IsNullOrWhiteSpace(updatedData.Operation))
            {
                return BadRequest("Invalid data provided.");
            }

            var existingData = _dbContext.user.FirstOrDefault(p => p.UserName == updatedData.UserName &&
                                                                   p.style == updatedData.style &&
                                                                   p.Operation == updatedData.Operation);

            if (existingData != null)
            {
                existingData.style = updatedData.style;
                existingData.Hand = updatedData.Hand;
                existingData.sew = updatedData.sew;
                existingData.Module = updatedData.Module;

                _dbContext.SaveChanges();
                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given user and style.");
        }

        /// <summary>
        /// Deletes user SMV data from the database
        /// </summary>
        /// <param name="deleteRequest">The criteria for deleting the data</param>
        /// <returns>Returns success message if data is deleted successfully</returns>
        [HttpPost]
        public IActionResult DeleteData([FromBody] DeleteRequest deleteRequest)
        {
            if (deleteRequest == null || string.IsNullOrWhiteSpace(deleteRequest.Slot) ||
                string.IsNullOrWhiteSpace(deleteRequest.style) || string.IsNullOrWhiteSpace(deleteRequest.Operation) ||
                string.IsNullOrWhiteSpace(deleteRequest.plant))
            {
                return BadRequest("Invalid data provided.");
            }

            var dataToDelete = _dbContext.user.FirstOrDefault(p => p.UserName == deleteRequest.Slot &&
                                                                   p.style == deleteRequest.style &&
                                                                   p.Operation == deleteRequest.Operation &&
                                                                   p.Plant == deleteRequest.plant);

            if (dataToDelete != null)
            {
                _dbContext.user.Remove(dataToDelete);
                _dbContext.SaveChanges();
                return Ok("Data deleted successfully");
            }

            return NotFound("Data not found for the given user and style.");
        }

        /// <summary>
        /// Model for deleting user SMV data
        /// </summary>
        public class DeleteRequest
        {
            /// <summary>
            /// The slot (username) to delete
            /// </summary>
            public string Slot { get; set; }

            /// <summary>
            /// The style to delete
            /// </summary>
            public string style { get; set; }

            /// <summary>
            /// The operation to delete
            /// </summary>
            public string Operation { get; set; }

            /// <summary>
            /// The plant to delete
            /// </summary>
            public string plant { get; set; }
        }

        /// <summary>
        /// Retrieves all user SMV data from the database
        /// </summary>
        /// <returns>Returns JSON array of all user SMV data</returns>
        [HttpGet]
        public IActionResult GetUserData()
        {
            var userData = _dbContext.user.ToList();
            return Json(userData);
        }

        /// <summary>
        /// Displays the machine details interface
        /// </summary>
        /// <returns>Returns the machine details view</returns>
        public IActionResult Index()
        {
            return View("~/Views/MachineDetails.cshtml");
        }

        /// <summary>
        /// Gets username suggestions based on a search term
        /// </summary>
        /// <param name="term">The search term to match usernames against</param>
        /// <returns>Returns JSON array of matching usernames</returns>
        [HttpGet]
        public IActionResult GetUsernameSuggestions(string term)
        {
            var suggestions = _dbContext.user
                .Where(u => u.UserName.Contains(term))
                .Select(u => u.UserName)
                .ToList();

            return Json(suggestions);
        }

        /// <summary>
        /// Searches for tack time data by module
        /// </summary>
        /// <param name="user">The module to search for</param>
        /// <returns>Returns JSON containing tack time for the module</returns>
        [HttpGet]
        public IActionResult SearchBySlot1(string user)
        {
            var slotData = _dbContext1.tack.FirstOrDefault(p => p.Module == user);

            if (slotData != null)
            {
                return Json(new { tack = slotData.Tack });
            }

            return Json(new { });
        }

        /// <summary>
        /// Adds new tack time data to the database
        /// </summary>
        /// <param name="newData">The new tack time data to add</param>
        /// <returns>Returns success message if data is added successfully</returns>
        [HttpPost]
        public IActionResult AddNewData1([FromBody] tack newData)
        {
            _dbContext1.tack.Add(newData);
            _dbContext1.SaveChanges();
            return Ok("Data added successfully");
        }

        /// <summary>
        /// Updates existing tack time data in the database
        /// </summary>
        /// <param name="updatedData">The updated tack time data</param>
        /// <returns>Returns success message if data is updated successfully</returns>
        [HttpPost]
        public IActionResult UpdateData1([FromBody] tack updatedData)
        {
            var existingData = _dbContext1.tack.FirstOrDefault(p => p.Module == updatedData.Module);

            if (existingData != null)
            {
                existingData.Tack = updatedData.Tack;
                _dbContext1.SaveChanges();
                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given module.");
        }

        /// <summary>
        /// Deletes tack time data from the database
        /// </summary>
        /// <param name="deleteRequest">The criteria for deleting the data</param>
        /// <returns>Returns success message if data is deleted successfully</returns>
        [HttpPost]
        public IActionResult DeleteData1([FromBody] DeleteRequest1 deleteRequest)
        {
            var dataToDelete = _dbContext1.tack.FirstOrDefault(p => p.Module == deleteRequest.Slot);

            if (dataToDelete != null)
            {
                _dbContext1.tack.Remove(dataToDelete);
                _dbContext1.SaveChanges();
                return Ok("Data deleted successfully");
            }

            return NotFound("Data not found for the given module.");
        }

        /// <summary>
        /// Model for deleting tack time data
        /// </summary>
        public class DeleteRequest1
        {
            /// <summary>
            /// The slot (module) to delete
            /// </summary>
            public string Slot { get; set; }
        }

        /// <summary>
        /// Retrieves all tack time data from the database
        /// </summary>
        /// <returns>Returns JSON array of all tack time data</returns>
        [HttpGet]
        public IActionResult GettackData()
        {
            var tackData = _dbContext1.tack.ToList();
            return Json(tackData);
        }

        /// <summary>
        /// Deletes style data from the database
        /// </summary>
        /// <param name="deleteRequest">The style data to delete</param>
        /// <returns>Returns success message if data is deleted successfully</returns>
        [HttpPost]
        public IActionResult DeleteData2([FromBody] style deleteRequest)
        {
            var dataToDelete = _dbContext2.KreedaIoT_styleDetails.FirstOrDefault(p => p.Style == deleteRequest.Style);

            if (dataToDelete != null)
            {
                _dbContext2.KreedaIoT_styleDetails.Remove(dataToDelete);
                _dbContext2.SaveChanges();
                return Ok("Data deleted successfully");
            }

            return NotFound("Data not found for the given style.");
        }

        /// <summary>
        /// Deletes style data from the database
        /// </summary>
        /// <param name="deleteRequest">The style data to delete</param>
        /// <returns>Returns success message if data is deleted successfully</returns>
        [HttpPost]
        public IActionResult DeleteData3([FromBody] style deleteRequest)
        {
            var dataToDelete = _dbContext2.KreedaIoT_styleDetails.FirstOrDefault(p => p.Style == deleteRequest.Style);

            if (dataToDelete != null)
            {
                _dbContext2.KreedaIoT_styleDetails.Remove(dataToDelete);
                _dbContext2.SaveChanges();
                return Ok("Data deleted successfully");
            }

            return NotFound("Data not found for the given style.");
        }

        /// <summary>
        /// Adds new style data to the database
        /// </summary>
        /// <param name="newData">The new style data to add</param>
        /// <returns>Returns success message if data is added successfully</returns>
        [HttpPost]
        public IActionResult AddNewData2([FromBody] style newData)
        {
            _dbContext2.KreedaIoT_styleDetails.Add(newData);
            _dbContext2.SaveChanges();
            return Ok("Data added successfully");
        }

        /// <summary>
        /// Updates existing style data in the database
        /// </summary>
        /// <param name="updateData">The updated style data</param>
        /// <returns>Returns success message if data is updated successfully</returns>
        [HttpPost]
        public IActionResult UpdateStyleData([FromBody] style updateData)
        {
            var existingData = _dbContext2.KreedaIoT_styleDetails.FirstOrDefault(p => p.Style == updateData.Style);

            if (existingData != null)
            {
                existingData.Style = updateData.Style;
                existingData.Operation = updateData.Operation;
                existingData.Plant = updateData.Plant;
                existingData.Module = updateData.Module;

                _dbContext2.SaveChanges();
                return Ok("Data updated successfully");
            }

            return NotFound("Data not found for the given style.");
        }

        /// <summary>
        /// Checks if a style and operation combination exists
        /// </summary>
        /// <param name="model">The model containing style and operation to check</param>
        /// <returns>Returns whether the combination exists</returns>
        [HttpPost]
        public IActionResult CheckStyleAndOperationExists([FromBody] StyleOperationCheckModel model)
        {
            bool exists = _dbContext2.KreedaIoT_styleDetails.Any(s => s.Style == model.Style && s.Operation == model.Operation);
            return Ok(new { exists });
        }

        /// <summary>
        /// Model for checking style and operation existence
        /// </summary>
        public class StyleOperationCheckModel
        {
            /// <summary>
            /// The style to check
            /// </summary>
            public string Style { get; set; }

            /// <summary>
            /// The operation to check
            /// </summary>
            public string Operation { get; set; }
        }

        /// <summary>
        /// Retrieves all style data from the database
        /// </summary>
        /// <returns>Returns JSON array of all style data</returns>
        [HttpGet]
        public IActionResult GetstyleData()
        {
            var styleData = _dbContext2.KreedaIoT_styleDetails.ToList();
            return Json(styleData);
        }
    }

    public class style
    {
        public int ID { get; set; }
        public string Style { get; set; }
        public string Operation { get; set; }
        public string Users { get; set; }
        public string Plant { get; set; } // Added the missing 'Plant' property
        public string Module { get; set; }
    }
}

