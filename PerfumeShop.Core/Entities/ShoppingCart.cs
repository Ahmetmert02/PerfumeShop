namespace PerfumeShop.Core.Entities
{
    public class ShoppingCart : BaseEntity
    {
        public int UserId { get; set; }
        public DateTime LastModified { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
    }
} 