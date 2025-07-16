using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Iot_dashboard.Controllers.GM
{
    public class GMController : Controller
    {
        // This action will handle /GM
        public IActionResult Index()
        {
            // Check for session variable set on successful login
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                // Not logged in, show login page
                return View("GMLogin");
            }

            // Logged in, show the menu
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            ViewBag.prvlgtyp = prvlgtyp;
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.station = HttpContext.Session.GetString("station");
            return View("GMIndex");
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var account = HttpContext.Session.GetString("account");
            var token = HttpContext.Session.GetString("accessToken");

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(token))
            {
                // Just clear session and redirect if missing info
                HttpContext.Session.Clear();
                return RedirectToAction("Index");
            }

            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/logout");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"account\":\"{account}\"}}", Encoding.UTF8, "application/json");

                try
                {
                    var response = await http.SendAsync(request);
                    // Ignore response details for now
                }
                catch
                {
                    // Optionally log error
                }
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> getStyles()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { styles = new string[0] });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/getStyles");
                request.Headers.Add("Authorization", "Bearer " + token);
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    // Parse the response and return the styles array
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("styles", out var stylesProp) && stylesProp.ValueKind == JsonValueKind.Array)
                    {
                        var styles = new System.Collections.Generic.List<string>();
                        foreach (var item in stylesProp.EnumerateArray())
                        {
                            styles.Add(item.GetString());
                        }
                        return Json(new { styles });
                    }
                    else
                    {
                        return Json(new { styles = new string[0] });
                    }
                }
                catch
                {
                    return Json(new { styles = new string[0] });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> getLiveStatus()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { LIVE = new[] { "Session expired or missing token." } });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5003/liveStatus");
                request.Headers.Add("Authorization", "Bearer " + token);
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return Json(new { LIVE = new[] { "No data received from external server." } });
                    }
                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(json);
                        return Json(new { LIVE = doc.RootElement });
                    }
                    catch (System.Exception ex)
                    {
                        return Json(new { LIVE = new[] { $"Error parsing JSON: {ex.Message}", $"Raw: {json}" } });
                    }
                }
                catch (System.Exception ex)
                {
                    return Json(new { LIVE = new[] { $"Error: {ex.Message}" } });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> getReport([FromBody] ReportRequest reqPayload)
        {
            // Validate request
            if (reqPayload == null)
            {
                return BadRequest("Invalid request");
            }

            DateTime? startDate = null, endDate = null;
            if (!string.IsNullOrEmpty(reqPayload.StartDate))
                startDate = DateTime.Parse(reqPayload.StartDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
            if (!string.IsNullOrEmpty(reqPayload.EndDate))
                endDate = DateTime.Parse(reqPayload.EndDate, null, System.Globalization.DateTimeStyles.RoundtripKind);

            // Build parameters
            var reqParam = new
            {
                reqPayload.Style,
                reqPayload.Size,
                DateFilter = reqPayload.DateFilter,
                StartDate = startDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                EndDate = endDate?.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Convert to JSON
            var jsonPayload = JsonSerializer.Serialize(reqParam);
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { Report = new[] { "Session expired or missing token." } });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/getReport");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{jsonPayload}", Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    return Json(new { Report = doc.RootElement });
                }
                catch (System.Exception ex)
                {
                    return Json(new { Report = new[] { $"Error: {ex.Message}" } });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> getMeasurements(string style)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { measurements = new[] { "Session expired or missing token." } });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/loadMData");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"Style\":\"{style}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    // Parse the received JSON string and wrap it in a 'measurements' property
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    return Json(new { measurements = doc.RootElement });
                }
                catch (System.Exception ex)
                {
                    return Json(new { measurements = new[] { $"Error: {ex.Message}" } });
                }
            }
        }

        [HttpPost]
        // This endpoint should return a plain array, not wrapped in an object.
        [HttpPost]
        public async Task<IActionResult> getAvailSizes(string style)
        {
            // Debug: Log entry into the method
            System.Diagnostics.Debug.WriteLine($"[getAvailSizes] Called with style: {style}");

            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[getAvailSizes] No access token found. Clearing session and returning empty array.");
                HttpContext.Session.Clear();
                // Return an empty array directly
                return Json(new string[0]);
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/getSizes");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"Style\":\"{style}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    System.Diagnostics.Debug.WriteLine("[getAvailSizes] Sending request to backend service for sizes...");
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[getAvailSizes] Received response: {json}");

                    // The response is a JSON array of strings, e.g. ["S", "XS"]
                    var sizes = System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
                    if (sizes == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[getAvailSizes] Deserialized sizes is null, returning empty array.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[getAvailSizes] Deserialized sizes: {string.Join(", ", sizes)}");
                    }
                    // Return the array directly, not wrapped in an object
                    return Json(sizes ?? new string[0]);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[getAvailSizes] Exception occurred: {ex.Message}");
                    // Return an empty array directly
                    return Json(new string[0]);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> deleteSize([FromBody] JsonElement payload)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            string style = payload.GetProperty("style").GetString();
            string size = payload.GetProperty("size").GetString();
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/deleteSize");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"style\":\"{style}\",\"size\":\"{size}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> getMeasurementTypes()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            using (var http = new HttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5003/getMeasurementTypes");
                req.Headers.Add("Authorization", "Bearer " + token);
                var res = await http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
        }


        [HttpGet]
        public IActionResult GMMeasure()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMMeasure");
        }

        [HttpGet]
        public IActionResult GMUsers()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 2)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMUsers");
        }

        [HttpGet]
        public IActionResult GMUpload()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 1)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMUpload");
        }

        [HttpGet]
        public IActionResult GMReferences()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 1)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMReferences");
        }

        [HttpGet]
        public IActionResult GMReport()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 1)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMReport");
        }

        [HttpGet]
        public IActionResult GMStyles()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 1)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = prvlgtyp;
            return View("GMStyles");
        }

        [HttpGet]
        public IActionResult GMDashboard()
        {
            var token = HttpContext.Session.GetString("accessToken");
            var prvlgtypStr = HttpContext.Session.GetString("type");
            int prvlgtyp = 0;
            int.TryParse(prvlgtypStr, out prvlgtyp);
            if (string.IsNullOrEmpty(token) || prvlgtyp < 1)
            {
                return RedirectToAction("Index");
            }
            ViewBag.account = HttpContext.Session.GetString("account");
            ViewBag.accessToken = HttpContext.Session.GetString("accessToken");
            ViewBag.plant = HttpContext.Session.GetString("plant");
            ViewBag.prvlgtyp = HttpContext.Session.GetString("type");
            return View("GMDashboard");
        }

        [HttpPost]
        public async Task<IActionResult> saveMData([FromBody] object payload)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/saveMData");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> insertStyleData([FromBody] object payload)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/insertStyleData");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> removeStyleM([FromBody] JsonElement payload)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            string style = payload.GetProperty("Style").GetString();
            string measurement = payload.GetProperty("Measurement").GetString();
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/removeStyleM");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"Style\":\"{style}\",\"Measurement\":\"{measurement}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> removeStyle([FromBody] JsonElement payload)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Session.Clear();
                return Json(new { success = false, message = "Session expired" });
            }
            string style = payload.GetProperty("Style").GetString();
            using (var http = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5003/removeStyle");
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Content = new StringContent($"{{\"Style\":\"{style}\"}}", Encoding.UTF8, "application/json");
                try
                {
                    var response = await http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // --- User Management Proxy Endpoints ---
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var response = await http.GetAsync("http://localhost:5003/getUsers");
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
        }

        public class AddUserRequest
        {
            public string username { get; set; }
            public string password { get; set; }
            public string prvlgtyp { get; set; }
            public string plant { get; set; }
            public string station { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest model)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            // 1. Get the public key
            string publicKey;
            using (var http = new HttpClient())
            {
                var keyResponse = await http.GetAsync("http://localhost:5003/publicKey");
                if (!keyResponse.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Failed to get public key" });
                publicKey = await keyResponse.Content.ReadAsStringAsync();
            }
            // 2. Encrypt the password using RSA
            string encryptedPassword;
            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                rsa.ImportFromPem(publicKey.ToCharArray());
                var encryptedBytes = rsa.Encrypt(System.Text.Encoding.UTF8.GetBytes(model.password), System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                encryptedPassword = System.Convert.ToBase64String(encryptedBytes);
            }
            // 3. Call the addUser API
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var payload = new
                {
                    username = model.username,
                    password = encryptedPassword,
                    prvlgtyp = model.prvlgtyp,
                    plant = model.plant,
                    station = model.station
                };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await http.PostAsync("http://localhost:5003/addUser", content);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
        }

        public class DeleteUserRequest
        {
            public string username { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest model)
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
                return Unauthorized();
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { username = model.username }), System.Text.Encoding.UTF8, "application/json");
                var response = await http.PostAsync("http://localhost:5003/removeUser", content);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
        }

        public class ReportRequest
        {
            public string Style { get; set; }
            public string Size { get; set; }
            public string DateFilter { get; set; }
            public string StartDate { get; set; } // Change to string
            public string EndDate { get; set; }   // Change to string
        }

    }
}