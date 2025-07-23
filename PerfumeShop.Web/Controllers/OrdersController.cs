using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Models;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IApiService _apiService;

        public OrdersController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            await UpdateCartItemCount();

            // Get user's orders
            var orders = await _apiService.GetUserOrdersAsync(userSession.UserId);
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Hole Warenkorb-Informationen für die Anzeige des Zählers
            await UpdateCartItemCount();

            // Get order
            var order = await _apiService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Verify that the order belongs to the user
            if (order.UserId != userSession.UserId)
            {
                return Forbid();
            }

            return View(order);
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