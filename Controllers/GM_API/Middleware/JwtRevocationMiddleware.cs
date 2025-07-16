using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Iot_dashboard.Controllers;

namespace Iot_dashboard.Controllers.GM_API.Middleware
{
    public class JwtRevocationMiddleware
    {
        private readonly RequestDelegate _next;
        // Reference to the revocation list (should be the same as used in your controller)
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = LoginController.RevokedTokens;

        public JwtRevocationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (_revokedTokens.TryGetValue(token, out var expiry))
                {
                    if (expiry > DateTime.UtcNow)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token has been revoked.");
                        return;
                    }
                    else
                    {
                        // Optionally remove expired entries
                        _revokedTokens.TryRemove(token, out _);
                    }
                }
            }

            await _next(context);
        }
    }
}