using Microsoft.AspNetCore.Mvc;
using Iot_dashboard.Controllers.GM_API.Models;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Add this using directive
using System.Collections.Concurrent; // Add this at the top

namespace Iot_dashboard.Controllers.GM_API
{
    [ApiController]
    [Route("api/gm/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private const string PrivateKeyCacheKey = "PrivateKeyPem";
        private static readonly ConcurrentBag<string> _tokenCacheKeys = new(); // Add this line
                                                                               // Add the following field to resolve the CS0103 error
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();
        public static ConcurrentDictionary<string, DateTime> RevokedTokens => _revokedTokens; // Add this property

        public LoginController(IConfiguration config, IMemoryCache cache)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (request == null) // Check for null request
            {
                return BadRequest(new LoginResponse { Success = false, Message = "Request cannot be null." });
            }

            // 1. Decode password using RSA private key
            string decodedPassword;
            try
            {
                decodedPassword = DecryptPassword(request.Password ?? string.Empty); // Handle possible null Password
            }
            catch
            {
                return BadRequest(new LoginResponse { Success = false, Message = "Invalid password encoding." });
            }

            // Hash password using MD5
            string hashedPassword;
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(decodedPassword);
                var hashBytes = md5.ComputeHash(inputBytes);
                hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            // 2. Authenticate user from Azure SQL DB
            string? connectionString = _config.GetConnectionString("hanger"); // Use nullable type for connectionString
           
            if (string.IsNullOrEmpty(connectionString))
            {
                return StatusCode(500, new LoginResponse { Success = false, Message = "Database connection string is not configured." });
            }

            string station = null;
            string plant = null;
            string PrivilegeType = null;
            bool isAuthenticated = false;

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "SELECT station, plant, prvlgtyp FROM CODE.hanger_sys.GM_USERS WHERE username = @Account AND password = @Password",
                    conn);
                cmd.Parameters.AddWithValue("@Account", request.Account ?? string.Empty); // Handle possible null Account
                cmd.Parameters.AddWithValue("@Password", hashedPassword);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        isAuthenticated = true;
                        station = reader["station"]?.ToString();
                        plant = reader["plant"]?.ToString();
                        PrivilegeType = reader["prvlgtyp"]?.ToString() ?? string.Empty; // Handle possible null PrivilegeType
                    }
                }
            }

            if (!isAuthenticated)
            {
                return Unauthorized(new LoginResponse { Success = false, Message = "Invalid credentials." });
            }

            // 3. Generate JWT token (add plant)
            var token = GenerateJwtToken(request.Account ?? string.Empty, PrivilegeType, plant); // Handle possible null Account

            // Store token in cache and add key to the list
            var tokenCacheKey = $"token_{request.Account}";
            _cache.Set(tokenCacheKey, token, TimeSpan.FromHours(1));
            _tokenCacheKeys.Add(tokenCacheKey);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                AccessToken = token,
                Station = station,
                Plant = plant,
                Type = PrivilegeType,
            });
        }

        [HttpGet("getUser")]
        [Authorize]
        public IActionResult GetUser()
        {
            RefreshTokenExpiry();
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 3)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 3 or above can view users." });
            }
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var userPlant = string.Empty;
            var connectionString = _config.GetConnectionString("hanger");
            var users = new List<object>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Get logged in user's plant
                using (var cmd = new SqlCommand("SELECT plant FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    var result = cmd.ExecuteScalar();
                    userPlant = result?.ToString() ?? string.Empty;
                }
                string query;
                if (privilegeLevel < 5)
                {
                    query = "SELECT username, plant, prvlgtyp FROM CODE.hanger_sys.GM_USERS WHERE plant = @plant";
                }
                else
                {
                    query = "SELECT username, plant, prvlgtyp FROM CODE.hanger_sys.GM_USERS";
                }
                using (var cmd = new SqlCommand(query, connection))
                {
                    if (privilegeLevel < 5)
                        cmd.Parameters.AddWithValue("@plant", userPlant);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new {
                                username = reader.GetString(0),
                                plant = reader.GetString(1),
                                prvlgtyp = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return Ok(new { Users = users });
        }

        [HttpPost("addUser")]
        [Authorize]
        public IActionResult AddUser([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            // Only users with privilege level 3 or above can add users
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 3)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 3 or above can add users." });
            }
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var userPlant = string.Empty;
            var connectionString = _config.GetConnectionString("hanger");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Get logged in user's plant
                using (var cmd = new SqlCommand("SELECT plant FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    var result = cmd.ExecuteScalar();
                    userPlant = result?.ToString() ?? string.Empty;
                }
            }
            // Parse request body
            if (!body.TryGetProperty("username", out var usernameElement) || usernameElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'username' property." });
            if (!body.TryGetProperty("password", out var passwordElement) || passwordElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'password' property." });
            if (!body.TryGetProperty("prvlgtyp", out var prvlgtypElement) || prvlgtypElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'prvlgtyp' property." });
            if (!body.TryGetProperty("plant", out var plantElement) || plantElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'plant' property." });
            if (!body.TryGetProperty("station", out var stationElement) || stationElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'station' property." });

            var newUsername = usernameElement.GetString();
            var encryptedPassword = passwordElement.GetString();
            var newPrvlgtyp = prvlgtypElement.GetString();
            var newPlant = plantElement.GetString();
            var station = stationElement.GetString();

            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(encryptedPassword) ||
                string.IsNullOrWhiteSpace(newPrvlgtyp) || string.IsNullOrWhiteSpace(newPlant) || string.IsNullOrWhiteSpace(station))
            {
                return BadRequest(new { Message = "All fields are required." });
            }

            if (!int.TryParse(newPrvlgtyp, out int newUserLevel))
            {
                return BadRequest(new { Message = "Invalid privilege type for new user." });
            }

            // Restrict plant and privilege level for non-lvl5
            if (privilegeLevel < 5)
            {
                if (!string.Equals(newPlant, userPlant, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new { Message = "You can only add users to your own plant." });
                }
                if (newUserLevel > privilegeLevel)
                {
                    return StatusCode(403, new { Message = "You can only add users with privilege level less than or equal to yours." });
                }
            }

            // Decrypt password using RSA private key
            string decryptedPassword;
            try
            {
                decryptedPassword = DecryptPassword(encryptedPassword);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Password decryption failed. Please ensure /publicKey endpoint has been called at least once after app start.", Details = ex.Message });
            }

            // Hash password using MD5
            string hashedPassword;
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(decryptedPassword);
                var hashBytes = md5.ComputeHash(inputBytes);
                hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if user exists
                using (var checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    checkCmd.Parameters.AddWithValue("@username", newUsername);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return Conflict(new { Message = "User already exists." });
                    }
                }

                // Insert new user
                using (var insertCmd = new SqlCommand(
                    @"INSERT INTO CODE.hanger_sys.GM_USERS (username, password, prvlgtyp, plant, station)
                      VALUES (@username, @password, @prvlgtyp, @plant, @station)", connection))
                {
                    insertCmd.Parameters.AddWithValue("@username", newUsername);
                    insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                    insertCmd.Parameters.AddWithValue("@prvlgtyp", newPrvlgtyp);
                    insertCmd.Parameters.AddWithValue("@plant", newPlant);
                    insertCmd.Parameters.AddWithValue("@station", station);

                    insertCmd.ExecuteNonQuery();
                }
            }

            return Ok(new { Message = "User added successfully." });
        }

        [HttpPost("removeUser")]
        [Authorize]
        public IActionResult RemoveUser([FromBody] JsonElement body)
        {
            RefreshTokenExpiry();
            // Only users with privilege level 3 or above can remove users
            var privilegeType = User.Claims.FirstOrDefault(c => c.Type == "prvlgtyp")?.Value;
            if (string.IsNullOrEmpty(privilegeType) || !int.TryParse(privilegeType, out int privilegeLevel) || privilegeLevel < 3)
            {
                return StatusCode(403, new { Message = "Only users with privilege level 3 or above can remove users." });
            }
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var userPlant = string.Empty;
            var connectionString = _config.GetConnectionString("hanger");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Get logged in user's plant
                using (var cmd = new SqlCommand("SELECT plant FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    var result = cmd.ExecuteScalar();
                    userPlant = result?.ToString() ?? string.Empty;
                }
            }
            if (!body.TryGetProperty("username", out var usernameElement) || usernameElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'username' property." });

            var removeUsername = usernameElement.GetString();
            if (string.IsNullOrWhiteSpace(removeUsername))
                return BadRequest(new { Message = "'username' cannot be empty." });

            // Get plant and privilege of user to be removed
            string removePlant = null;
            int removeLevel = 0;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand("SELECT plant, prvlgtyp FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    cmd.Parameters.AddWithValue("@username", removeUsername);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            removePlant = reader.GetString(0);
                            int.TryParse(reader.GetString(1), out removeLevel);
                        }
                        else
                        {
                            return NotFound(new { Message = "User not found." });
                        }
                    }
                }
            }
            // Restrict plant and privilege level for non-lvl5
            if (privilegeLevel < 5)
            {
                if (!string.Equals(removePlant, userPlant, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new { Message = "You can only remove users from your own plant." });
                }
                if (removeLevel > privilegeLevel)
                {
                    return StatusCode(403, new { Message = "You can only remove users with privilege level less than or equal to yours." });
                }
            }

            var cacheKeysToRemove = _tokenCacheKeys
                .Where(key => key.Contains(removeUsername, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in cacheKeysToRemove)
            {
                if (_cache.TryGetValue(key, out string token) && !string.IsNullOrEmpty(token))
                {
                    // Try to read expiry from the token
                    var handler = new JwtSecurityTokenHandler();
                    JwtSecurityToken jwtToken = null;
                    try
                    {
                        jwtToken = handler.ReadJwtToken(token);
                    }
                    catch { /* ignore invalid tokens */ }

                    DateTime expiry = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(2);
                    RevokeToken(token, expiry);
                }
                _cache.Remove(key);
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                // Remove user
                using (var deleteCmd = new SqlCommand(
                    "DELETE FROM CODE.hanger_sys.GM_USERS WHERE username = @username", connection))
                {
                    deleteCmd.Parameters.AddWithValue("@username", removeUsername);
                    deleteCmd.ExecuteNonQuery();
                }
            }

            return Ok(new { Message = "User and any active tokens removed and revoked successfully." });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout([FromBody] JsonElement body)
        {
            // Get username from request body
            if (!body.TryGetProperty("account", out var accountElement) || accountElement.ValueKind != JsonValueKind.String)
                return BadRequest(new { Message = "Missing or invalid 'account' property." });

            var username = accountElement.GetString();
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { Message = "'account' cannot be empty." });

            // Find and revoke all tokens for this user
            var cacheKeysToRemove = _tokenCacheKeys
                .Where(key => key.Contains(username, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in cacheKeysToRemove)
            {
                if (_cache.TryGetValue(key, out string token) && !string.IsNullOrEmpty(token))
                {
                    // Try to read expiry from the token
                    var handler = new JwtSecurityTokenHandler();
                    JwtSecurityToken jwtToken = null;
                    try
                    {
                        jwtToken = handler.ReadJwtToken(token);
                    }
                    catch { /* ignore invalid tokens */ }

                    DateTime expiry = jwtToken?.ValidTo ?? DateTime.UtcNow.AddHours(2);
                    RevokeToken(token, expiry);
                }
                _cache.Remove(key);
            }

            return Ok(new { Message = "User logged out and token(s) revoked." });
        }

        private string DecryptPassword(string base64Password)
        {
            if (!_cache.TryGetValue(PrivateKeyCacheKey, out string privateKeyPemString))
                throw new InvalidOperationException("Private key not found in cache.");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPemString);
            byte[] encryptedBytes = Convert.FromBase64String(base64Password);
            byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private string GenerateJwtToken(string username, string PrivilegeType, string plant)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? string.Empty)); // Handle possible null Jwt:Key
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? string.Empty, // Handle possible null Jwt:Issuer
                audience: _config["Jwt:Audience"] ?? string.Empty, // Handle possible null Jwt:Audience
                claims: new[] {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("prvlgtyp", PrivilegeType),
                    new Claim("plant", plant ?? string.Empty)
                },
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // When removing a user or logging out, add their token to the revoked list
        // Example method to revoke a token:
        public static void RevokeToken(string token, DateTime expiry)
        {
            RevokedTokens[token] = expiry;
        }

        private void RefreshTokenExpiry()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = null;
                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch { return; }

                var username = jwtToken?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(username))
                {
                    var tokenCacheKey = $"token_{username}";
                    if (_cache.TryGetValue(tokenCacheKey, out string cachedToken) && cachedToken == token)
                    {
                        // Reset sliding expiration to 2 hours
                        _cache.Set(tokenCacheKey, token, TimeSpan.FromHours(1));
                    }
                }
            }
        }
    }
}