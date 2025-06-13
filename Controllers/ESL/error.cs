using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Reflection;

namespace Iot_dashboard.Controllers.ESL
{
    public class ErrorController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/ESL/errorproof.cshtml");
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
                    string json = "{\"account\": \"error1\",\"loginType\": 3,\"password\": \"" + encryptedPassword + "\"}";

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

        static void AddItem(string snum, string Module, string op, string p1, string p2, string p3, string s1, string s2, string s3, string d1, string d2, string d3, string authorizationKey)
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
                            barCode = snum,
                            attrCategory = "ERROR_PROOF",
                            attrName = "mec_error",
                            productCode = snum,

                            nfcUrl = snum,
                            custFeature1 = op,
                            custFeature2 = Module,
                            custFeature3 = p1,
                            custFeature4 = p2,
                            custFeature5 = p3,
                            custFeature6 = s1,
                            custFeature7 = s2,
                            custFeature8 = s3,
                            custFeature9 = d1,
                            custFeature10 = d2,
                            custFeature11 = d3
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
                        Console.WriteLine($"SUCCESS for {snum}");
                    }
                    else
                    {
                        Console.WriteLine($"FAIL for {snum}");
                    }
                }
            }
            catch (WebException e)
            {
                throw new Exception($"An error occurred while adding an item: {e.Message}");
            }
        }

        [HttpPost]
        public IActionResult AddNewItem([FromBody] ItemData itemData)
        {
            try
            {
                string newPassword = "Error@123";
                string encryptedPassword = GetRsaCredentials(newPassword);
                string authorizationKey = PerformLogin(encryptedPassword);


                string p1 = "", p2 = "", p3 = "";
                string s1 = "", s2 = "", s3 = "";
                string d1 = "", d2 = "", d3 = "";


                int pickAndPlaceValue = int.TryParse(itemData.PickAndPlace, out int tempPickAndPlace) ? tempPickAndPlace : 0;
                int sewingValue = int.TryParse(itemData.Sewing, out int tempSewing) ? tempSewing : 0;
                int disposeValue = int.TryParse(itemData.Dispose, out int tempDispose) ? tempDispose : 0;


                if (pickAndPlaceValue == 1)
                {
                    p1 = "X";
                }
                else if (pickAndPlaceValue == 2)
                {
                    p2 = "X";
                }
                else if (pickAndPlaceValue == 3)
                {
                    p3 = "X";
                }


                if (sewingValue == 1)
                {
                    s1 = "X";
                }
                else if (sewingValue == 2)
                {
                    s2 = "X";
                }
                else if (sewingValue == 3)
                {
                    s3 = "X";
                }


                if (disposeValue == 1)
                {
                    d1 = "X";
                }
                else if (disposeValue == 2)
                {
                    d2 = "X";
                }
                else if (disposeValue == 3)
                {
                    d3 = "X";
                }


                AddItem(itemData.Snum, itemData.Module, itemData.Op, p1, p2, p3, s1, s2, s3, d1, d2, d3, authorizationKey);

                return Json(new { status = "Success" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "Error", message = ex.Message });
            }
        }


        public class ItemData
        {
            public string Snum { get; set; }
            public string Module { get; set; }
            public string Op { get; set; }
            public string PickAndPlace { get; set; }
            public string Sewing { get; set; }
            public string Dispose { get; set; }
        }
    }
}
