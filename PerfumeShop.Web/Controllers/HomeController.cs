using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Models;
using PerfumeShop.Web.Services;
using System.Diagnostics;

namespace PerfumeShop.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApiService _apiService;

        public HomeController(ILogger<HomeController> logger, IApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Hole Warenkorb-Informationen für die Anzeige des Zählers
                await UpdateCartItemCount();
                
                var products = await _apiService.GetAsync<List<Product>>("api/Products");
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for the homepage");
                ViewBag.CartItemCount = 0; // Setze auf 0 im Fehlerfall
                return View(new List<Product>());
            }
        }

        public IActionResult Privacy()
        {
            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            UpdateCartItemCount().Wait();
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        public IActionResult DebugToken()
        {
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                ViewBag.TokenInfo = "No user session found";
                return View();
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);
            var token = userSession?.Token;

            if (string.IsNullOrEmpty(token))
            {
                ViewBag.TokenInfo = "No token found in session";
                return View();
            }

            ViewBag.Token = token;
            ViewBag.TokenInfo = "Token found in session";
            
            return View();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen des Warenkorb-Zählers");
                ViewBag.CartItemCount = 0;
            }
        }
    }
}
