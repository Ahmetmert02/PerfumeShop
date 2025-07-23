using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    public class HomeController : BaseAdminController
    {
        private readonly IApiService _apiService;

        public HomeController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Collect statistics for the dashboard
            var products = await _apiService.GetProductsAsync();
            var categories = await _apiService.GetCategoriesAsync();
            var brands = await _apiService.GetBrandsAsync();
            
            ViewBag.TotalProducts = products.Count();
            ViewBag.ActiveProducts = products.Count(p => p.IsActive);
            ViewBag.TotalCategories = categories.Count();
            ViewBag.TotalBrands = brands.Count();

            return View();
        }
    }
} 