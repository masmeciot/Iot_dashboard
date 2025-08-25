using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Iot_dashboard.Controllers.GM_API
{
    [ApiController]
    [Route("api/gm/[controller]")]
    public class publicKey : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private const string PublicKeyCacheKey = "PublicKeyPem";
        private const string PrivateKeyCacheKey = "PrivateKeyPem";

        public publicKey(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (!_cache.TryGetValue(PublicKeyCacheKey, out string publicKeyPemString) ||
                !_cache.TryGetValue(PrivateKeyCacheKey, out string privateKeyPemString))
            {
                using var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportSubjectPublicKeyInfo();
                var publicKeyPem = PemEncoding.Write("PUBLIC KEY", publicKey);
                publicKeyPemString = new string(publicKeyPem);

                var privateKey = rsa.ExportPkcs8PrivateKey();
                var privateKeyPem = PemEncoding.Write("PRIVATE KEY", privateKey);
                privateKeyPemString = new string(privateKeyPem);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(24));

                _cache.Set(PublicKeyCacheKey, publicKeyPemString, cacheEntryOptions);
                _cache.Set(PrivateKeyCacheKey, privateKeyPemString, cacheEntryOptions);
            }

            return Content(publicKeyPemString, "text/plain", Encoding.UTF8);
        }

        // Helper for RSA decryption using private key from config or file
        public static string DecryptWithPrivateKey(string base64Encrypted, IConfiguration config, IMemoryCache cache = null)
        {
            string privateKeyPem = null;

            // Try to get from cache if available
            if (cache != null && cache.TryGetValue(PrivateKeyCacheKey, out string cachedKey))
            {
                privateKeyPem = cachedKey;
            }
            else
            {
                // Fallback to config
                privateKeyPem = config["Jwt:PrivateKey"];
            }

            if (string.IsNullOrWhiteSpace(privateKeyPem))
                throw new Exception("Private key not configured. Please call /publicKey endpoint at least once after app start.");

            byte[] encryptedBytes = Convert.FromBase64String(base64Encrypted);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(privateKeyPem.ToCharArray());
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}