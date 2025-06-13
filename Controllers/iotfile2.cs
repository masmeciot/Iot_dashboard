using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers
{
    /// <summary>
    /// Controller responsible for handling IoT firmware file uploads and downloads
    /// </summary>
    public class iotfile2Controller : Controller
    {
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Constructor that initializes the web host environment for file operations
        /// </summary>
        /// <param name="environment">The web host environment service</param>
        public iotfile2Controller(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Displays the file upload interface
        /// </summary>
        /// <returns>Returns the file upload view</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/fileupload2.cshtml");
        }

        /// <summary>
        /// Handles the file upload process for IoT firmware updates
        /// </summary>
        /// <returns>Returns a JSON response indicating success or failure of the upload</returns>
        [HttpPost("iotfile2/UploadFile")]
        public async Task<IActionResult> UploadFile()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Define the path to save the file
            var folderPath = Path.Combine(_environment.WebRootPath, "iotfile2");
            var filePath = Path.Combine(folderPath, "update.bin");

            // Ensure the folder exists
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Generate the public link
            var fileUrl = $"{Request.Scheme}://{Request.Host}/iotfile2/update.bin";

            return Ok(new { Message = "File uploaded successfully.", FileUrl = fileUrl });
        }

        /// <summary>
        /// Handles the download of the uploaded firmware file
        /// </summary>
        /// <returns>Returns the firmware file as a downloadable binary file</returns>
        [HttpGet("iotfile2/update.bin")]
        public IActionResult DownloadFile()
        {
            var folderPath = Path.Combine(_environment.WebRootPath, "iotfile2");
            var filePath = Path.Combine(folderPath, "update.bin");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            return PhysicalFile(filePath, "application/octet-stream", "update.bin");
        }
    }
}
