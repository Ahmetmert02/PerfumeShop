using PerfumeShop.Core.Entities;

namespace PerfumeShop.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Product> Products { get; }
        IRepository<Category> Categories { get; }
        IRepository<Brand> Brands { get; }
        IRepository<Order> Orders { get; }
        IRepository<OrderItem> OrderItems { get; }
        IRepository<ShoppingCart> ShoppingCarts { get; }
        IRepository<CartItem> CartItems { get; }
        IRepository<User> Users { get; }

        IRepository<T> Repository<T>() where T : BaseEntity;
        
        Task<int> CompleteAsync();
    }
} 