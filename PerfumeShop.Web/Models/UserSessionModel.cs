namespace PerfumeShop.Web.Models
{
    public class UserSessionModel
    {
        public int UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Token { get; set; }
    }
} 