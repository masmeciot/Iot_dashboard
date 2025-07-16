using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using Iot_dashboard.Controllers.GM_API.Middleware;

namespace GM_Core_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddSwaggerGen();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddMemoryCache();

            // Add JWT authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
                    };
                });

            // Add authorization
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseMiddleware<JwtRevocationMiddleware>(); // Add this line
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Ensure RSA keypair is generated and cached at startup
            using (var scope = app.Services.CreateScope())
            {
                var provider = scope.ServiceProvider;
                var config = provider.GetRequiredService<IConfiguration>();
                var cache = provider.GetRequiredService<IMemoryCache>();

                // Try to get the private key; if not present, call the publicKey endpoint to generate it
                if (!cache.TryGetValue("PrivateKeyPem", out string _))
                {
                    // Use HttpClient to call the publicKey endpoint locally
                    var client = new HttpClient();
                    var url = config["PublicKeyEndpointUrl"] ?? "http://localhost:5000/publicKey";
                    try
                    {
                        var _ = client.GetStringAsync(url).Result;
                    }
                    catch
                    {
                        // Ignore errors; the endpoint may be hit on first real request
                    }
                }
            }

            // Add this before app.Run();
            //app.Urls.Add("http://0.0.0.0:5002"); // Listen on all network interfaces, port 5000
            // Optionally, also listen on HTTPS if you have a certificate
            app.Urls.Add("http://0.0.0.0:5003");

            app.Run();
        }
    }
}
