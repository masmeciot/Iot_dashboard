using Iot_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static Iot_dashboard.Controllers.Iot.Iotconfig;

namespace Iot_dashboard.Controllers.ESL
{
    public class SubModuleESL : Controller
    {
        public class AppDbContext65 : DbContext
        {
            public DbSet<moduledata> ESLModuleData { get; set; }

            public AppDbContext65(DbContextOptions<AppDbContext65> options) : base(options)
            {
            }
        }

        private readonly AppDbContext65 _dbContext;

        public SubModuleESL(AppDbContext65 dbContext)
        {
            _dbContext = dbContext;



        }


        static string GetPublicKey()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json;charset=utf-8");
                    string response = client.DownloadString("http://esl-eu.zkong.com:9999/user/getErpPublicKey");
                    dynamic jsonResponse = JsonConvert.DeserializeObject(response);

                    if (jsonResponse != null && jsonResponse.data != null)
                    {
                        return jsonResponse.data;
                    }
                    else
                    {
                        throw new Exception("Invalid response format: 'data' not found.");
                    }
                }
            }
            catch (WebException e)
            {
                throw new Exception($"An error occurred while fetching the public key: {e.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred: {ex.Message}");
            }
        }

        static string GetPemFormat(string publicKey)
        {
            string key = "";
            int chunkSize = 64;
            int length = publicKey.Length;

            for (int startPos = 0; startPos < length; startPos += chunkSize)
            {
                int remainingLength = length - startPos;
                int currentChunkSize = Math.Min(chunkSize, remainingLength);
                string keyLen = publicKey.Substring(startPos, currentChunkSize);
                key += keyLen + "\n";
            }

            return "-----BEGIN PUBLIC KEY-----\n" + key + "-----END PUBLIC KEY-----";
        }

        static string GetRsaCredentials(string newPass)
        {
            string publicKey = GetPublicKey();
            string pemFormat = GetPemFormat(publicKey);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(pemFormat);
                byte[] encryptedPass = rsa.Encrypt(Encoding.UTF8.GetBytes(newPass), RSAEncryptionPadding.Pkcs1);
                return Convert.ToBase64String(encryptedPass);
            }
        }

        static string PerformLogin(string encryptedPassword)
        {
            try
            {
                string publicKeyText = GetPublicKey();

                if (!string.IsNullOrEmpty(publicKeyText))
                {
                    string url = "http://esl-eu.zkong.com:9999/user/login";
                    string json = "{\"account\": \"MainAssembly\",\"loginType\": 3,\"password\": \"" + encryptedPassword + "\"}";

                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json;charset=utf-8");
                        string response = client.UploadString(url, "POST", json);
                        dynamic responseData = JsonConvert.DeserializeObject(response);
                        return responseData.data.token;
                    }
                }
                else
                {
                    throw new Exception("Failed to retrieve public key.");
                }
            }
            catch (WebException e)
            {
                throw new Exception($"An error occurred during login: {e.Message}");
            }
        }






        static void AddItem(string module1, string module2, string soli1, string style1, string cw1, string cutb1, string inqty1, string fgq1, string delivery1,
                       string soli2, string style2, string cw2, string cutb2, string inqty2, string fgq2, string delivery2, string authorizationKey)
        {
            try
            {
                TimeZoneInfo SriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime SriLankaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SriLankaTimeZone);
                string currentDateTime = SriLankaTime.ToString("yyyy-MM-dd HH:mm:ss");

                var jsonPayload = new
                {
                    agencyId = "1558577702698",
                    merchantId = "1695194880541",
                    itemList = new[]
                    {
                new
                {
                    barCode = "MEC-MD-SUB",
                    attrCategory = "MASKreeda",
                    attrName = "MASTemplate",
                    productCode = "MEC-MD-SUB",
                    nfcUrl = "MEC-MD-SUB",
                    custFeature18 = "SUB ASSEMBLY",
                    custFeature1 = module1,
                    custFeature2 = module2,
                    custFeature3 = soli1,
                    custFeature4 = style1,
                    custFeature5 = cw1,
                    custFeature6 = cutb1,
                    custFeature7 = inqty1,
                    custFeature8 = fgq1,
                    custFeature9 = delivery1,
                    custFeature10 = soli2,
                    custFeature11 = style2,
                    custFeature12 = cw2,
                    custFeature13 = cutb2,
                    custFeature14 = inqty2,
                    custFeature15 = fgq2,
                    custFeature16 = delivery2,
                    custFeature17 = currentDateTime
                }
            }
                };

                string jsonData = JsonConvert.SerializeObject(jsonPayload);

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json;charset=utf-8");
                    client.Headers.Add("Authorization", authorizationKey);

                    string response = client.UploadString("http://esl-eu.zkong.com:9999/item/batchImportItem", "POST", jsonData);

                    Console.WriteLine(jsonData);

                    if (response.ToUpper().Contains("SUCCESS"))
                    {
                        Console.WriteLine("SUCCESS for module1 and module2");
                        Console.WriteLine(jsonData);
                    }
                    else
                    {
                        Console.WriteLine("FAIL for module1 and module2");
                    }
                }
            }
            catch (WebException e)
            {
                throw new Exception($"An error occurred while adding an item: {e.Message}");
            }
        }







        [HttpPost]
        public IActionResult SaveModuleData([FromBody] List<moduledata> modulesData)
        {
            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            //    return BadRequest(string.Join("; ", errors));
            //}


            string newPassword = "Main12345";
            string encryptedPassword = GetRsaCredentials(newPassword);
            string authorizationKey = PerformLogin(encryptedPassword);

            // Validate input: must have exactly 2 modules, and required fields must not be null or empty
            if (modulesData == null || modulesData.Count != 2 ||
                string.IsNullOrWhiteSpace(modulesData[0].Module) ||
                string.IsNullOrWhiteSpace(modulesData[1].Module))
            {
                return BadRequest("Invalid data! Please check if all the fields are filled...");
            }

            var module1 = modulesData[0];
            var module2 = modulesData[1];

            // Save or update the data for each module
            foreach (var newData in modulesData)
            {
                var existingData = _dbContext.ESLModuleData
                    .FirstOrDefault(m => m.Module == newData.Module);

                if (existingData == null)
                {
                    // Ensure all int? fields are set correctly (null or int)
                    var dataToAdd = new moduledata
                    {
                        Module = newData.Module,
                        SOLI = newData.SOLI,
                        Style = newData.Style,
                        cw = newData.cw,
                        cutb = newData.cutb,
                        inqty = newData.inqty,
                        fgq = newData.fgq,
                        delivery = newData.delivery
                    };
                    _dbContext.ESLModuleData.Add(dataToAdd);
                }
                else
                {
                    existingData.SOLI = newData.SOLI;
                    existingData.Style = newData.Style;
                    existingData.cw = newData.cw;
                    existingData.cutb = newData.cutb;
                    existingData.inqty = newData.inqty;
                    existingData.fgq = newData.fgq;
                    existingData.delivery = newData.delivery;
                }
            }

            _dbContext.SaveChanges();

            AddItem(
                module1.Module,
                module2.Module,
                module1.SOLI,
                module1.Style,
                module1.cw ?? string.Empty,
                module1.cutb?.ToString() ?? string.Empty,
                module1.inqty?.ToString() ?? string.Empty,
                module1.fgq?.ToString() ?? string.Empty,
                module1.delivery ?? string.Empty,
                module2.SOLI,
                module2.Style,
                module2.cw ?? string.Empty,
                module2.cutb?.ToString() ?? string.Empty,
                module2.inqty?.ToString() ?? string.Empty,
                module2.fgq?.ToString() ?? string.Empty,
                module2.delivery ?? string.Empty,
                authorizationKey
            );

            return Ok();
        }




        [HttpGet]
        public IActionResult GetModuleData(string moduleId)
        {
            var moduleData = _dbContext.ESLModuleData
                .Where(m => m.Module == moduleId)
                .Select(m => new
                {
                    soli = m.SOLI,
                    style = m.Style,
                    cw = m.cw,
                    bqty = m.cutb,
                    iqty = m.inqty,
                    fgqty = m.fgq,
                    dqty = m.delivery
                })
                .FirstOrDefault();

            if (moduleData == null)
            {
                return NoContent(); // Returns 204 No Content 
            }

            return Ok(moduleData);
        }





        public IActionResult Index()
        {
            return View("~/Views/ESL/submoduleesl.cshtml");
        }
    }
}
