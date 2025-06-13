using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.Iot
{
    /// <summary>
    /// Controller responsible for handling IoT firmware file uploads and downloads
    /// </summary>
    public class IotfileController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// Constructor that initializes the web host environment
        /// </summary>
        /// <param name="environment">The web host environment for file operations</param>
        public IotfileController(IWebHostEnvironment environment)
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
            return View("~/Views/Fileupload.cshtml");
        }

        /// <summary>
        /// Handles the upload of IoT firmware files
        /// </summary>
        /// <returns>Returns a success message and the public URL of the uploaded file</returns>
        [HttpPost("iotfile/UploadFile")]
        public async Task<IActionResult> UploadFile()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Define the path to save the file
            var folderPath = Path.Combine(_environment.WebRootPath, "iotfile");
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
            var fileUrl = $"{Request.Scheme}://{Request.Host}/iotfile/update.bin";

            return Ok(new { Message = "File uploaded successfully.", FileUrl = fileUrl });
        }

        /// <summary>
        /// Handles the download of IoT firmware files
        /// </summary>
        /// <returns>Returns the firmware file as a binary stream</returns>
        [HttpGet("iotfile/update.bin")]
        public IActionResult DownloadFile()
        {
            var folderPath = Path.Combine(_environment.WebRootPath, "iotfile");
            var filePath = Path.Combine(folderPath, "update.bin");

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found.");

            return PhysicalFile(filePath, "application/octet-stream", "update.bin");
        }
    }
}
