using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Models;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Services
{
    public interface IApiService
    {
        // Generic API method
        Task<T> GetAsync<T>(string endpoint) where T : class;
        
        // Authentication
        Task<WebAuthResponseModel> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(User user);

        // Products
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<bool> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(int id, Product product);
        Task<bool> DeleteProductAsync(int id);

        // Categories
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(int id, Category category);
        Task<bool> DeleteCategoryAsync(int id);
        
        // Brands
        Task<IEnumerable<Brand>> GetBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int id);
        Task<bool> CreateBrandAsync(Brand brand);
        Task<bool> UpdateBrandAsync(int id, Brand brand);
        Task<bool> DeleteBrandAsync(int id);
        
        // Orders
        Task<IEnumerable<Order>> GetOrdersAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task<Order> CreateOrderAsync(Order order);
        Task<bool> UpdateOrderStatusAsync(int id, OrderStatus status);
        
        // Shopping Cart
        Task<ShoppingCartResponse> GetShoppingCartAsync();
        Task<ShoppingCart> CreateShoppingCartAsync(ShoppingCart shoppingCart);
        Task<bool> AddToCartAsync(CartItem cartItem);
        Task<bool> UpdateCartItemQuantityAsync(int cartItemId, int quantity, int userId);
        Task<bool> RemoveFromCartAsync(int cartItemId, int userId);
        Task<bool> ClearCartAsync();
    }
} 