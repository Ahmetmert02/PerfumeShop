using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using PerfumeShop.Repository.Data;
using PerfumeShop.Repository.Repositories;

namespace PerfumeShop.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IRepository<Product>? _products;
        private IRepository<Category>? _categories;
        private IRepository<Brand>? _brands;
        private IRepository<Order>? _orders;
        private IRepository<OrderItem>? _orderItems;
        private IRepository<ShoppingCart>? _shoppingCarts;
        private IRepository<CartItem>? _cartItems;
        private IRepository<User>? _users;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
        public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
        public IRepository<Brand> Brands => _brands ??= new Repository<Brand>(_context);
        public IRepository<Order> Orders => _orders ??= new Repository<Order>(_context);
        public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
        public IRepository<ShoppingCart> ShoppingCarts => _shoppingCarts ??= new Repository<ShoppingCart>(_context);
        public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
        public IRepository<User> Users => _users ??= new Repository<User>(_context);

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            return new Repository<T>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
} 