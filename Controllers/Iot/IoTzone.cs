using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    public class IoTzone : Controller
    {
        public class AppDbContext40 : DbContext
        {
            public DbSet<zone> KreedaIoT_ZoneDetails { get; set; }

            public AppDbContext40(DbContextOptions<AppDbContext40> options) : base(options)
            {
            }
        }

        private readonly AppDbContext40 _dbContext;

        public IoTzone(AppDbContext40 dbContext)
        {
            _dbContext = dbContext;
        }

     /*   public IActionResult Index()
        {
            return View("~/Views/zone.cshtml");
        }*/

        public IActionResult GetSuggestions()
        {
            var data = _dbContext.KreedaIoT_ZoneDetails
                .Select(z => new
                {
                    z.style,
                    z.operationZone
                })
                .Distinct()
                .ToList();

            return Ok(data);
        }



        [HttpGet]
        public async Task<IActionResult> Search(string style, string op)
        {
            var zones = await _dbContext.KreedaIoT_ZoneDetails
                .Where(z => z.style == style && z.operationZone == op)
                .ToListAsync();

            if (zones.Any())
            {
                var response = new
                {
                    success = true,
                    data = new
                    {
                        style = zones.First().style,
                        operationZone = zones.First().operationZone,
                        zones = zones.Select(z => new
                        {
                            z.Zone,
                            z.Stich,
                            z.Operation,
                            z.Wait
                        })
                    }
                };
                return Json(response);
            }
            return Json(new { success = false });
        }


        [HttpPost]
        public async Task<IActionResult> AddZone( string style, string op, string plant, string user, List<ZoneData> Zones)
        {
            if (ModelState.IsValid)
            {
                foreach (var zone in Zones)
                {
                    var newZone = new zone
                    {
                       
                        Zone = zone.ZoneName,
                        Stich = zone.StitchCount,
                        Operation = zone.Operation,
                        Wait = zone.WaitingTime,
                        style = style,
                        operationZone = op,
                        Plant = plant,
                        UserName = user
                    };

                    _dbContext.KreedaIoT_ZoneDetails.Add(newZone);
                }

                await _dbContext.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }



        [HttpPost]
        public async Task<IActionResult> SaveChanges(string style, string op, string plant, string user, List<ZoneData> Zones)
        {
            Console.WriteLine("Received Data:");
            Console.WriteLine($"Style: {style}");
            Console.WriteLine($"Operation: {op}");
            Console.WriteLine($"Plant: {plant}");
            Console.WriteLine($"UserName: {user}");

            if (ModelState.IsValid)
            {
                var existingZones = _dbContext.KreedaIoT_ZoneDetails.Where(z => z.style == style && z.operationZone==op);
                _dbContext.KreedaIoT_ZoneDetails.RemoveRange(existingZones);

                foreach (var zone in Zones)
                {
                    var updatedZone = new zone
                    {
                      
                        Zone = zone.ZoneName,
                        Stich = zone.StitchCount,
                        Operation = zone.Operation,
                        Wait = zone.WaitingTime,
                        style = style,
                        operationZone = op,
                        Plant = plant,
                       UserName = user
                    };

                    _dbContext.KreedaIoT_ZoneDetails.Add(updatedZone);
                }

                await _dbContext.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string style, string op)
        {
            var zones = _dbContext.KreedaIoT_ZoneDetails.Where(z => z.style == style && z.operationZone == op);
            _dbContext.KreedaIoT_ZoneDetails.RemoveRange(zones);
            await _dbContext.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    public class ZoneData
    {
        public string ZoneName { get; set; }
        public int StitchCount { get; set; }
        public string Operation { get; set; }
        public int WaitingTime { get; set; }

    }
}
