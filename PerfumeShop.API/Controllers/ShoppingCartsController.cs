using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using System.Text.Json;
using System.Security.Claims;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShoppingCartsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/ShoppingCarts
        [HttpGet]
        public async Task<ActionResult<ShoppingCartResponse>> GetUserCart()
        {
            try
            {
                // Benutzer-ID aus dem Token abrufen
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return BadRequest("Ungültige Benutzer-ID");
                }

                // Warenkorb des Benutzers abrufen
                var carts = await _unitOfWork.ShoppingCarts.FindAsync(sc => sc.UserId == userId);
                var cart = carts.FirstOrDefault();

                // Wenn kein Warenkorb existiert, einen leeren zurückgeben
                if (cart == null)
                {
                    return Ok(new ShoppingCartResponse
                    {
                        Id = 0,
                        UserId = userId,
                        LastModified = DateTime.Now,
                        CartItems = new List<CartItemResponse>(),
                        TotalItems = 0,
                        TotalPrice = 0
                    });
                }

                // Warenkorb-Artikel abrufen
                var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == cart.Id);
                var cartItemsList = cartItems.ToList();

                // Produkte für jeden Warenkorb-Artikel laden
                var cartItemResponses = new List<CartItemResponse>();
                decimal totalPrice = 0;
                int totalItems = 0;

                foreach (var item in cartItemsList)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        var itemResponse = new CartItemResponse
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            ProductName = product.Name,
                            ProductImage = product.ImageUrl,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.Quantity * item.UnitPrice
                        };

                        cartItemResponses.Add(itemResponse);
                        totalPrice += itemResponse.TotalPrice;
                        totalItems += item.Quantity;
                    }
                }

                // Warenkorb-Antwort erstellen
                var response = new ShoppingCartResponse
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    LastModified = cart.LastModified,
                    CartItems = cartItemResponses,
                    TotalItems = totalItems,
                    TotalPrice = totalPrice
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen des Warenkorbs: {ex.Message}");
                return StatusCode(500, new { message = "Interner Serverfehler beim Abrufen des Warenkorbs" });
            }
        }

        // POST: api/ShoppingCarts/items
        [HttpPost("items")]
        public async Task<ActionResult<CartItemResponse>> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                Console.WriteLine($"AddToCart API aufgerufen mit: {JsonSerializer.Serialize(request)}");

                if (request == null)
                {
                    return BadRequest("Warenkorbartikel kann nicht null sein");
                }
                
                // Validiere die Eingabedaten
                if (request.ProductId <= 0)
                {
                    return BadRequest("ProductId muss größer als 0 sein");
                }
                
                if (request.Quantity <= 0)
                {
                    return BadRequest("Quantity muss größer als 0 sein");
                }
                
                // Benutzer-ID aus dem Token abrufen
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return BadRequest("Ungültige Benutzer-ID");
                }
                
                // Produkt überprüfen
                var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
                if (product == null)
                {
                    return BadRequest($"Produkt mit ID {request.ProductId} nicht gefunden");
                }
                
                // Warenkorb des Benutzers abrufen oder erstellen
                var shoppingCarts = await _unitOfWork.ShoppingCarts.FindAsync(sc => sc.UserId == userId);
                var shoppingCart = shoppingCarts.FirstOrDefault();

                if (shoppingCart == null)
                {
                    // Neuen Warenkorb erstellen, wenn keiner existiert
                    shoppingCart = new ShoppingCart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(shoppingCart);
                    await _unitOfWork.CompleteAsync();
                }

                // Prüfen, ob das Produkt bereits im Warenkorb ist
                var existingItems = await _unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == shoppingCart.Id && ci.ProductId == request.ProductId);
                var existingItem = existingItems.FirstOrDefault();
                CartItem cartItem;

                if (existingItem != null)
                {
                    // Menge aktualisieren, wenn das Produkt bereits im Warenkorb ist
                    existingItem.Quantity += request.Quantity;
                    existingItem.UnitPrice = product.Price; // Preis aktualisieren, falls er sich geändert hat
                    _unitOfWork.CartItems.Update(existingItem);
                    cartItem = existingItem;
                }
                else
                {
                    // Neuen Warenkorbartikel hinzufügen, wenn das Produkt nicht im Warenkorb ist
                    cartItem = new CartItem
                    {
                        ShoppingCartId = shoppingCart.Id,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        UnitPrice = product.Price,
                        CreatedAt = DateTime.Now
                    };
                    await _unitOfWork.CartItems.AddAsync(cartItem);
                }

                // Datum der letzten Änderung des Warenkorbs aktualisieren
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();

                // Antwort erstellen
                var response = new CartItemResponse
                {
                    Id = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = product.Name,
                    ProductImage = product.ImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.Quantity * cartItem.UnitPrice
                };

                return CreatedAtAction(nameof(GetUserCart), response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Hinzufügen zum Warenkorb: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Interner Serverfehler beim Hinzufügen zum Warenkorb" });
            }
        }

        // PUT: api/ShoppingCarts/items/{id}
        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                if (request == null || request.Quantity < 0)
                {
                    return BadRequest("Ungültige Anfrage");
                }

                // Benutzer-ID aus dem Token abrufen
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return BadRequest("Ungültige Benutzer-ID");
                }

                // Warenkorbartikel abrufen
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(id);
                if (cartItem == null)
                {
                    return NotFound("Warenkorbartikel nicht gefunden");
                }

                // Warenkorb abrufen
                var shoppingCart = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartItem.ShoppingCartId);
                if (shoppingCart == null)
                {
                    return NotFound("Warenkorb nicht gefunden");
                }

                // Überprüfen, ob der Warenkorb dem Benutzer gehört
                if (shoppingCart.UserId != userId)
                {
                    return Forbid();
                }

                if (request.Quantity == 0)
                {
                    // Artikel entfernen, wenn die Menge 0 ist
                    _unitOfWork.CartItems.Remove(cartItem);
                }
                else
                {
                    // Menge aktualisieren
                    cartItem.Quantity = request.Quantity;
                    _unitOfWork.CartItems.Update(cartItem);
                }

                // Datum der letzten Änderung des Warenkorbs aktualisieren
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Aktualisieren des Warenkorbs: {ex.Message}");
                return StatusCode(500, new { message = "Interner Serverfehler beim Aktualisieren des Warenkorbs" });
            }
        }

        // DELETE: api/ShoppingCarts/items/{id}
        [HttpDelete("items/{id}")]
        public async Task<IActionResult> RemoveCartItem(int id)
        {
            try
            {
                // Benutzer-ID aus dem Token abrufen
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return BadRequest("Ungültige Benutzer-ID");
                }

                // Warenkorbartikel abrufen
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(id);
                if (cartItem == null)
                {
                    return NotFound("Warenkorbartikel nicht gefunden");
                }

                // Warenkorb abrufen
                var shoppingCart = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartItem.ShoppingCartId);
                if (shoppingCart == null)
                {
                    return NotFound("Warenkorb nicht gefunden");
                }

                // Überprüfen, ob der Warenkorb dem Benutzer gehört
                if (shoppingCart.UserId != userId)
                {
                    return Forbid();
                }

                // Artikel entfernen
                _unitOfWork.CartItems.Remove(cartItem);

                // Datum der letzten Änderung des Warenkorbs aktualisieren
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Entfernen des Warenkorbs: {ex.Message}");
                return StatusCode(500, new { message = "Interner Serverfehler beim Entfernen des Warenkorbs" });
            }
        }

        // DELETE: api/ShoppingCarts
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                // Benutzer-ID aus dem Token abrufen
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return BadRequest("Ungültige Benutzer-ID");
                }

                // Warenkorb des Benutzers abrufen
                var carts = await _unitOfWork.ShoppingCarts.FindAsync(sc => sc.UserId == userId);
                var cart = carts.FirstOrDefault();

                if (cart == null)
                {
                    return NotFound("Warenkorb nicht gefunden");
                }

                // Alle Warenkorbartikel abrufen
                var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == cart.Id);

                // Alle Warenkorbartikel entfernen
                _unitOfWork.CartItems.RemoveRange(cartItems);

                // Datum der letzten Änderung des Warenkorbs aktualisieren
                cart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(cart);

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Leeren des Warenkorbs: {ex.Message}");
                return StatusCode(500, new { message = "Interner Serverfehler beim Leeren des Warenkorbs" });
            }
        }
    }

    // DTO-Klassen
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }

    public class CartItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ShoppingCartResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LastModified { get; set; }
        public List<CartItemResponse> CartItems { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 