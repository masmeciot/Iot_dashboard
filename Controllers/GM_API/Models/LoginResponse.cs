namespace Iot_dashboard.Controllers.GM_API.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccessToken { get; set; }
        public string Station { get; set; }
        public string Plant { get; set; }
        public string Type { get; set; }
    }
}