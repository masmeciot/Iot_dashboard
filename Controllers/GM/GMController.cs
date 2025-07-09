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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5003/logout");
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5003/getStyles");
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
                var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:5003/liveStatus");
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5003/loadMData");
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

        public IActionResult GMDashboard()
        {
            var token = HttpContext.Session.GetString("accessToken");
            if (string.IsNullOrEmpty(token))
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5003/saveMData");
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
    }
}