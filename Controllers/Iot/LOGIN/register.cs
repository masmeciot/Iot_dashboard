using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Iot_dashboard.Models;
using static Iot_dashboard.Controllers.Iot.Iotconfig;
using Microsoft.AspNetCore.Identity;


namespace Iot_dashboard.Controllers.Iot.LOGIN
{

    public class Register : Controller
    {

        public class AppDbContext68 : DbContext
        {
            public DbSet<KreedaIotUser> KreedaIotUser { get; set; }

            public AppDbContext68(DbContextOptions<AppDbContext68> options) : base(options)
            {
            }
        }

        private readonly AppDbContext68 _dbContext;

        public Register(AppDbContext68 dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/LOGIN/register.cshtml");
        }



        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.Plant))
                {
                    return Json(new { success = false, message = "All fields are required." });
                }

                // Check if the username already exists
                var existingUser = await _dbContext.KreedaIotUser.FirstOrDefaultAsync(u => u.username == model.Username);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = $"The username '{model.Username}' is already taken." });
                }

                // Hash the password
                var passwordHasher = new PasswordHasher<KreedaIotUser>();
                var hashedPassword = passwordHasher.HashPassword(null, model.Password);

                // Save the user to the database
                var newUser = new KreedaIotUser
                {
                    username = model.Username,
                    password = hashedPassword,
                    plant = model.Plant
                };

                _dbContext.KreedaIotUser.Add(newUser);
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }



        public class UserRegistrationModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Plant { get; set; }
        }


    }
}





