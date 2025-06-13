using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Iot_dashboard.Models;
using static Iot_dashboard.Controllers.Iot.MachineController;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling IoT dashboard data operations
    /// </summary>
    public class IoTdashController : Controller
    {
        /// <summary>
        /// Database context for managing IoT dashboard data
        /// </summary>
        public class AppDbContext33 : DbContext
        {
            public DbSet<iotdash> iot { get; set; }

            public AppDbContext33(DbContextOptions<AppDbContext33> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<iotdash>().HasNoKey(); 
            }
        }
        private readonly AppDbContext33 _dbContext;

        /// <summary>
        /// Constructor that initializes the database context
        /// </summary>
        /// <param name="dbContext">The database context for IoT dashboard data</param>
        public IoTdashController(AppDbContext33 dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves and processes IoT dashboard data for MEC plant
        /// </summary>
        /// <returns>Returns JSON containing processed dashboard data grouped by module</returns>
        [HttpGet]
        public IActionResult Data()
        {
            var groupedData = _dbContext.iot
                .Where(x => x.Plant == "MEC" && x.DeltaTime<=70000 && x.TotalRuntime <= 70000)
                .GroupBy(x => new { x.style, x.UserName, x.Operation })
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList();

            var result = groupedData
                .GroupBy(x => x.Module)
                .OrderBy(g => ExtractNumericPart(g.Key)) 
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new
                    {
                        UserName = x.UserName,
                        DeltaAvg = Math.Round(x.DeltaTime / 1000.0, 0),
                        RunAvg = Math.Round(x.TotalRuntime / 1000.0, 0),
                        Hand = x.Hand,
                        Sew = x.Sew
                    }).ToArray()
                );

            return Json(result);
        }

        /// <summary>
        /// Extracts the numeric part from a module name
        /// </summary>
        /// <param name="moduleName">The module name to extract from</param>
        /// <returns>Returns the numeric part of the module name</returns>
        private int ExtractNumericPart(string moduleName)
        {
            var numericPart = new string(moduleName.Where(char.IsDigit).ToArray());
            return int.TryParse(numericPart, out var number) ? number : 0;
        }

        /// <summary>
        /// Displays the IoT dashboard interface
        /// </summary>
        /// <returns>Returns the IoT dashboard view</returns>
        public IActionResult Index()
        {
            return View("~/Views/IoTdashboard.cshtml");
        }

        /*   [HttpGet]
   public IActionResult FOF1()
   {
       var latestData = new List<object>
   {
       new { userName = "Deshani", deltaAvg = 15, runAvg = 8, hand = 10, sew = 8 },
       new { userName = "Lihini", deltaAvg = 20, runAvg = 12, hand = 19, sew = 10 },
        new { userName ="Danushi", deltaAvg = 18, runAvg = 17, hand = 18, sew = 11 },
       new { userName = "Manjula", deltaAvg = 18, runAvg = 11, hand = 10, sew = 8 },
       new { userName = "Shermila", deltaAvg = 39, runAvg = 30, hand = 25, sew = 20 },

   };

       return Json(latestData);
   }*/




        /* [HttpGet]

      public IActionResult FOF1()
        {
            var latestData = _dbContext.iot
                .Where(x => x.Module == "FOF1")
                .GroupBy(x => x.UserName)
                .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                .ToList()
                .Select(x => new
                {
                    UserName = x.UserName,
                    DeltaAvg = x.Delta,
                    RunAvg = x.Run,
                    Hand = x.Hand,
                    Sew = x.Sew
                })
                .ToList();

            return Json(latestData);
         }*/



        /* 
       [HttpGet]
            public IActionResult FOF1()
            {
                var latestData = _dbContext.iot
                    .Where(x => x.Module == "FOF1")
                    .ToList() // Materialize the query result
                    .GroupBy(x => x.UserName)
                    .Select(group => new
                    {
                        UserName = group.Key,
                        DeltaRanges = GetRangesWithCount(group.Select(x => x.Delta)),
                        RunRanges = GetRangesWithCount(group.Select(x => x.Run))
                    })
                    .ToList();

                foreach (var data in latestData)
                {
                    Console.WriteLine($"User: {data.UserName}");
                    Console.WriteLine("Delta Ranges:");
                    PrintRanges(data.DeltaRanges);
                    Console.WriteLine("Run Ranges:");
                    PrintRanges(data.RunRanges);
                    Console.WriteLine();
                }

                return Json(latestData);
            }

            private List<(int, int, int)> GetRangesWithCount(IEnumerable<int> values)
            {
                var sortedValues = values.OrderBy(x => x).ToList();
                var rangesWithCount = new List<(int, int, int)>();
                var count = sortedValues.Count;

                int start = 0;
                for (int i = 1; i < count; i++)
                {
                    if (Math.Abs(sortedValues[i] - sortedValues[i - 1]) > 5)
                    {
                        rangesWithCount.Add((sortedValues[start], sortedValues[i - 1], i - start));
                        start = i;
                    }
                }

                if (start < count)
                {
                    rangesWithCount.Add((sortedValues[start], sortedValues[count - 1], count - start));
                }

                return rangesWithCount;
            }

            private void PrintRanges(List<(int, int, int)> ranges)
            {
                foreach (var range in ranges)
                {
                    Console.WriteLine($"Range: {range.Item1}-{range.Item2}, Count: {range.Item3}");
                }
            }
       */






        /*    [HttpGet]
            public IActionResult FOF2()
            {
                var latestData = _dbContext.iot
                    .Where(x => x.Module == "FOF2")
                    .GroupBy(x => x.UserName)
                    .Select(g => g.OrderByDescending(x => x.Time).FirstOrDefault())
                    .ToList()
                    .Select(x => new
                    {
                        UserName = x.UserName,
                        DeltaAvg = x.Delta,
                        RunAvg = x.Run,
                        Hand = x.Hand,
                        Sew = x.Sew
                    })
                    .ToList();

                return Json(latestData);
            }
        */

        /*  [HttpGet]
          public IActionResult FOF2()
          {
              var latestData = new List<object>
          {
              new { userName = "Pavithra", deltaAvg = 10, runAvg = 11, hand = 8, sew = 12 },
              new { userName = "Kalani", deltaAvg = 28, runAvg = 20, hand = 21, sew = 14 },
               new { userName ="Divya", deltaAvg = 36, runAvg = 20, hand = 20, sew = 15 },
              new { userName = "Mayuri", deltaAvg = 18, runAvg = 11, hand = 15, sew = 11 },

          };

              return Json(latestData);
          }*/


        /*    public IActionResult FOF2()
            {
                var filteredData = _dbContext.iot.Where(x => x.Module == "FOF2")
                                                  .GroupBy(x => x.UserName)
                                                  .Select(g => new
                                                  {
                                                      UserName = g.Key,
                                                      DeltaAvg = Math.Round((double?)(g.Where(x => x.Delta != null).Average(x => x.Delta)) ?? 0),
                                                      RunAvg = Math.Round((double?)(g.Where(x => x.Run != null).Average(x => x.Run)) ?? 0),
                                                      Hand = g.FirstOrDefault().Hand,
                                                      Sew = g.FirstOrDefault().Sew
                                                  }).ToList();

                var dataArray = filteredData.Select(x => new
                {
                    UserName = x.UserName,
                    DeltaAvg = x.DeltaAvg,
                    RunAvg = x.RunAvg,
                    Hand = x.Hand,
                    Sew = x.Sew
                }).ToArray();

                return Json(dataArray);
            }*/




    }
}
