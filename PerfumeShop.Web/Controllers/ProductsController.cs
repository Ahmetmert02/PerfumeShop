using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IApiService _apiService;
        private const int PageSize = 9;

        public ProductsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Products
        public async Task<IActionResult> Index(
            int? categoryId = null, 
            string brandId = null, 
            string searchTerm = null, 
            string sort = null, 
            decimal? minPrice = null, 
            decimal? maxPrice = null, 
            int page = 1)
        {
            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            await UpdateCartItemCount();
            
            // Load all products
            var allProducts = await _apiService.GetProductsAsync();
            
            // Load categories and brands for filtering
            var categories = await _apiService.GetCategoriesAsync();
            var brands = await _apiService.GetBrandsAsync();
            
            // Filter by category and brand if specified
            var filteredProducts = allProducts.AsEnumerable();
            
            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                filteredProducts = filteredProducts.Where(p => 
                    p.Name.ToLower().Contains(searchTerm) || 
                    p.Description.ToLower().Contains(searchTerm) ||
                    (p.Brand != null && p.Brand.Name.ToLower().Contains(searchTerm)) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(searchTerm))
                );
                ViewBag.CurrentSearchTerm = searchTerm;
            }
            
            // Filter by category
            if (categoryId.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CurrentCategoryId = categoryId.Value;
            }
            
            // Filter by brand
            if (!string.IsNullOrWhiteSpace(brandId))
            {
                var brandIds = brandId.Split(',').Select(int.Parse).ToList();
                filteredProducts = filteredProducts.Where(p => brandIds.Contains(p.BrandId));
                ViewBag.CurrentBrandId = brandId;
            }
            
            // Filter by price range
            if (minPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);
                ViewBag.CurrentMinPrice = minPrice.Value;
            }
            
            if (maxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);
                ViewBag.CurrentMaxPrice = maxPrice.Value;
            }
            
            // Sort products
            switch (sort)
            {
                case "name":
                    filteredProducts = filteredProducts.OrderBy(p => p.Name);
                    break;
                case "price-asc":
                    filteredProducts = filteredProducts.OrderBy(p => p.Price);
                    break;
                case "price-desc":
                    filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
                    break;
                default:
                    filteredProducts = filteredProducts.OrderBy(p => p.Id);
                    break;
            }
            ViewBag.CurrentSort = sort;
            
            // Pagination
            var totalItems = filteredProducts.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            
            filteredProducts = filteredProducts
                .Skip((page - 1) * PageSize)
                .Take(PageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
            
            return View(filteredProducts.ToList());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            await UpdateCartItemCount();
            
            var product = await _apiService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        
        // Hilfsmethode zum Aktualisieren des Warenkorb-Zählers
        private async Task UpdateCartItemCount()
        {
            try
            {
                // Prüfe, ob der Benutzer angemeldet ist
                var userSessionJson = HttpContext.Session.GetString("UserSession");
                if (!string.IsNullOrEmpty(userSessionJson))
                {
                    // Hole Warenkorb-Informationen
                    var shoppingCartResponse = await _apiService.GetShoppingCartAsync();
                    ViewBag.CartItemCount = shoppingCartResponse?.TotalItems ?? 0;
                }
                else
                {
                    ViewBag.CartItemCount = 0;
                }
            }
            catch (Exception)
            {
                ViewBag.CartItemCount = 0;
            }
        }
    }
} 