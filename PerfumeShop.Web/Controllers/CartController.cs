using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Models;
using PerfumeShop.Web.Services;
using System.Text;

namespace PerfumeShop.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IApiService _apiService;

        public CartController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get shopping cart via API
                var shoppingCartResponse = await _apiService.GetShoppingCartAsync();
                
                // Setze die Anzahl der Artikel im Warenkorb in ViewBag
                ViewBag.CartItemCount = shoppingCartResponse?.TotalItems ?? 0;
                
                return View(shoppingCartResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden des Warenkorbs: {ex.Message}");
                TempData["ErrorMessage"] = "Fehler beim Laden des Warenkorbs. Bitte versuchen Sie es später erneut.";
                
                // Zeige einen leeren Warenkorb an
                var emptyCart = new ShoppingCartResponse();
                
                // Setze die Anzahl der Artikel im Warenkorb auf 0
                ViewBag.CartItemCount = 0;
                
                return View(emptyCart);
            }
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Get product
            var product = await _apiService.GetProductByIdAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            // Add to cart via API
            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                CreatedAt = DateTime.Now
            };

            var result = await _apiService.AddToCartAsync(cartItem);
            if (result)
            {
                TempData["SuccessMessage"] = "Product added to cart successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add product to cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Update cart item quantity via API
            var result = await _apiService.UpdateCartItemQuantityAsync(cartItemId, quantity, userSession.UserId);
            if (result)
            {
                TempData["SuccessMessage"] = "Cart updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Remove cart item via API
            var result = await _apiService.RemoveFromCartAsync(cartItemId, userSession.UserId);
            if (result)
            {
                TempData["SuccessMessage"] = "Product removed from cart.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove product from cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Clear cart via API
            var result = await _apiService.ClearCartAsync();
            if (result)
            {
                TempData["SuccessMessage"] = "Cart cleared successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to clear cart.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            // Get shopping cart via API
            var shoppingCartResponse = await _apiService.GetShoppingCartAsync();
            
            // Setze die Anzahl der Artikel im Warenkorb in ViewBag
            ViewBag.CartItemCount = shoppingCartResponse?.TotalItems ?? 0;
            
            if (shoppingCartResponse == null || shoppingCartResponse.CartItems == null || !shoppingCartResponse.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(shoppingCartResponse);
        }

        // POST: Cart/PlaceOrder
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string paymentMethod)
        {
            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Get shopping cart via API
            var shoppingCartResponse = await _apiService.GetShoppingCartAsync();
            
            // Setze die Anzahl der Artikel im Warenkorb in ViewBag
            ViewBag.CartItemCount = shoppingCartResponse?.TotalItems ?? 0;

            if (shoppingCartResponse == null || shoppingCartResponse.CartItems == null || !shoppingCartResponse.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction(nameof(Index));
            }

            // Make sure address and payment method are not empty
            if (string.IsNullOrEmpty(shippingAddress))
            {
                shippingAddress = "No address provided";
            }
            
            if (string.IsNullOrEmpty(paymentMethod))
            {
                paymentMethod = "Credit Card"; // Default value
            }
            
            Console.WriteLine($"Address used: {shippingAddress}");
            Console.WriteLine($"Payment method used: {paymentMethod}");

            // Create order
            var order = new Order
            {
                OrderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = shoppingCartResponse.TotalPrice,
                UserId = userSession.UserId,
                User = null, // Explizit auf null setzen, damit die API es nicht validiert
                ShippingAddress = shippingAddress,
                PaymentMethod = paymentMethod,
                CreatedAt = DateTime.Now,
                OrderItems = shoppingCartResponse.CartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPrice,
                    TotalPrice = ci.TotalPrice,
                    CreatedAt = DateTime.Now
                }).ToList()
            };

            Console.WriteLine($"Order created: {JsonConvert.SerializeObject(order)}");

            // Create order via API
            Order createdOrder;
            try
            {
                createdOrder = await _apiService.CreateOrderAsync(order);
                
                if (createdOrder != null)
                {
                    // Clear the cart after successful order
                    await _apiService.ClearCartAsync();
                    
                    // Nach erfolgreicher Bestellung den Warenkorb-Zähler auf 0 setzen
                    ViewBag.CartItemCount = 0;
                    
                    // Redirect to order confirmation
                    return RedirectToAction(nameof(OrderConfirmation), new { id = createdOrder.Id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create order. Please try again.";
                    return RedirectToAction(nameof(Checkout));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                TempData["ErrorMessage"] = $"Error creating order: {ex.Message}";
                return RedirectToAction(nameof(Checkout));
            }
        }

        // GET: Cart/OrderConfirmation/5
        [Authorize]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            // Get order via API
            var order = await _apiService.GetOrderByIdAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index", "Orders");
            }

            // Get user ID from session
            var userSessionJson = HttpContext.Session.GetString("UserSession");
            if (string.IsNullOrEmpty(userSessionJson))
            {
                // If not logged in, redirect to login
                return RedirectToAction("Login", "Account");
            }

            var userSession = JsonConvert.DeserializeObject<UserSessionModel>(userSessionJson);

            // Verify that the order belongs to the user
            if (order.UserId != userSession.UserId)
            {
                TempData["ErrorMessage"] = "You do not have permission to view this order.";
                return RedirectToAction("Index", "Orders");
            }
            
            // Setze die Anzahl der Artikel im Warenkorb auf 0 nach erfolgreicher Bestellung
            ViewBag.CartItemCount = 0;

            return View(order);
        }
    }
} 