using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Iot_dashboard.Controllers.GM
{
    [Route("GM/[controller]/[action]")]
    public class GMLoginHandler : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // 1. Get the public key from the API
            string publicKey;
            using (var http = new HttpClient())
            {
                http.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
                var keyResponse = await http.GetAsync("/api/gm/publicKey");
                if (!keyResponse.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Failed to get public key" });

                publicKey = await keyResponse.Content.ReadAsStringAsync();
            }

            // 2. Encrypt the password using RSA
            string encryptedPassword;
            using (var rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKey.ToCharArray());
                var encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(model.Password), RSAEncryptionPadding.Pkcs1);
                encryptedPassword = System.Convert.ToBase64String(encryptedBytes);
            }

            // 3. Call the login API with username and encrypted password
            using (var http = new HttpClient())
            {
                http.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
                var loginPayload = new
                {
                    account = model.Username,
                    password = encryptedPassword
                };
                var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
                var loginResponse = await http.PostAsync("/api/gm/login/login", content);

                var loginResult = await loginResponse.Content.ReadAsStringAsync();

                if (!loginResponse.IsSuccessStatusCode)
                {
                    // Try to extract a message from the API response
                    string errorMessage = "Login API failed";
                    try
                    {
                        using var doc = JsonDocument.Parse(loginResult);
                        if (doc.RootElement.TryGetProperty("message", out var msgProp))
                        {
                            errorMessage = msgProp.GetString();
                        }
                        else
                        {
                            errorMessage = loginResult;
                        }
                    }
                    catch
                    {
                        errorMessage = loginResult;
                    }
                    return Json(new { success = false, message = errorMessage });
                }

                // Parse the login result
                var loginJson = JsonDocument.Parse(loginResult).RootElement;
                if (loginJson.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    // Store values in session
                    HttpContext.Session.SetString("accessToken", loginJson.GetProperty("accessToken").GetString());
                    HttpContext.Session.SetString("station", loginJson.GetProperty("station").GetString());
                    HttpContext.Session.SetString("plant", loginJson.GetProperty("plant").GetString());
                    HttpContext.Session.SetString("type", loginJson.GetProperty("type").GetString());
                    // Store account/username for logout
                    HttpContext.Session.SetString("account", model.Username);

                    // Return redirect instruction to client
                    return Json(new { success = true, redirectUrl = Url.Action("", "GM", new { area = "" }) });
                }
                else
                {
                    string errorMessage = loginJson.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Login failed";
                    return Json(new { success = false, message = errorMessage });
                }
            }
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}