namespace PerfumeShop.Web.Models
{
    public class WebAuthResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsAdmin { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }
} 