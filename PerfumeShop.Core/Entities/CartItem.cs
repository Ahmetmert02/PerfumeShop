using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeShop.Core.Entities
{
    public class CartItem : BaseEntity
    {
        public int ShoppingCartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        [NotMapped]
        public int UserId { get; set; }

        // Navigation properties
        public virtual ShoppingCart? ShoppingCart { get; set; }
        
        [NotMapped]
        public virtual Product? Product { get; set; }
    }
} 