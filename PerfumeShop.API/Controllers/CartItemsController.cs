using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using System.Text.Json;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartItemsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartItemsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/CartItems/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItem>> GetCartItem(int id)
        {
            try
            {
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(id);
                if (cartItem == null)
                {
                    return NotFound();
                }

                // Verify that the user is accessing their own cart item
                var shoppingCart = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartItem.ShoppingCartId);
                if (shoppingCart == null)
                {
                    return NotFound();
                }

                if (!User.IsInRole("Admin") && int.Parse(User.FindFirst("UserId")?.Value ?? "0") != shoppingCart.UserId)
                {
                    return Forbid();
                }

                // Load product details
                cartItem.Product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);

                return cartItem;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart item: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while getting cart item" });
            }
        }

        // POST: api/CartItems
        [HttpPost]
        public async Task<ActionResult<CartItem>> AddToCart([FromBody] CartItemCreateDto cartItemDto)
        {
            try
            {
                Console.WriteLine($"AddToCart API called with CartItemDto: {JsonSerializer.Serialize(cartItemDto)}");

                if (cartItemDto == null)
                {
                    return BadRequest("Cart item cannot be null");
                }
                
                // Validiere die Eingabedaten
                if (cartItemDto.ProductId <= 0)
                {
                    Console.WriteLine("Fehler: ProductId muss größer als 0 sein");
                    return BadRequest("ProductId must be greater than 0");
                }
                
                if (cartItemDto.Quantity <= 0)
                {
                    Console.WriteLine("Fehler: Quantity muss größer als 0 sein");
                    return BadRequest("Quantity must be greater than 0");
                }
                
                if (cartItemDto.UnitPrice <= 0)
                {
                    Console.WriteLine("Fehler: UnitPrice muss größer als 0 sein");
                    return BadRequest("UnitPrice must be greater than 0");
                }
                
                if (cartItemDto.UserId <= 0)
                {
                    Console.WriteLine("Fehler: UserId muss größer als 0 sein");
                    return BadRequest("UserId must be greater than 0");
                }
                
                // Erstelle ein CartItem aus dem DTO
                var cartItem = new CartItem
                {
                    ProductId = cartItemDto.ProductId,
                    Quantity = cartItemDto.Quantity,
                    UnitPrice = cartItemDto.UnitPrice,
                    UserId = cartItemDto.UserId,
                    CreatedAt = DateTime.Now
                };

                // Ermittle die UserId aus dem Token
                var userId = int.Parse(User.FindFirst("nameidentifier")?.Value ?? "0");
                Console.WriteLine($"Token UserId: {userId}, CartItem UserId: {cartItem.UserId}");
                
                // Überschreibe die UserId im CartItem mit der UserId aus dem Token
                // Jeder Benutzer (auch Admin) kann nur in seinen eigenen Warenkorb einkaufen
                cartItem.UserId = userId;
                Console.WriteLine($"UserId im CartItem auf {userId} gesetzt");

                // Get or create shopping cart for the user
                var shoppingCarts = await _unitOfWork.ShoppingCarts.FindAsync(sc => sc.UserId == cartItem.UserId);
                var shoppingCart = shoppingCarts.FirstOrDefault();
                Console.WriteLine($"Vorhandener Warenkorb gefunden: {shoppingCart != null}");

                if (shoppingCart == null)
                {
                    // Create a new shopping cart if none exists
                    shoppingCart = new ShoppingCart
                    {
                        UserId = cartItem.UserId,
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now
                    };
                    await _unitOfWork.ShoppingCarts.AddAsync(shoppingCart);
                    await _unitOfWork.CompleteAsync();
                    Console.WriteLine($"Neuer Warenkorb erstellt mit ID: {shoppingCart.Id}");
                }

                // Check if the product exists
                var product = await _unitOfWork.Products.GetByIdAsync(cartItem.ProductId);
                if (product == null)
                {
                    Console.WriteLine($"Fehler: Produkt mit ID {cartItem.ProductId} nicht gefunden");
                    return BadRequest($"Product with ID {cartItem.ProductId} not found");
                }
                Console.WriteLine($"Produkt gefunden: {product.Name}, Preis: {product.Price}");

                // Check if the product is already in the cart
                var existingItems = await _unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == shoppingCart.Id && ci.ProductId == cartItem.ProductId);
                var existingItem = existingItems.FirstOrDefault();
                Console.WriteLine($"Produkt bereits im Warenkorb: {existingItem != null}");

                if (existingItem != null)
                {
                    // Update quantity if the product is already in the cart
                    existingItem.Quantity += cartItem.Quantity;
                    existingItem.UnitPrice = cartItem.UnitPrice; // Update price in case it changed
                    _unitOfWork.CartItems.Update(existingItem);
                    Console.WriteLine($"Warenkorbartikel aktualisiert, neue Menge: {existingItem.Quantity}");
                }
                else
                {
                    // Add new cart item if the product is not in the cart
                    cartItem.ShoppingCartId = shoppingCart.Id;
                    await _unitOfWork.CartItems.AddAsync(cartItem);
                    Console.WriteLine($"Neuer Warenkorbartikel hinzugefügt");
                }

                // Update shopping cart last modified date
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();
                Console.WriteLine("Änderungen erfolgreich gespeichert");

                return CreatedAtAction(nameof(GetCartItem), new { id = existingItem?.Id ?? cartItem.Id }, existingItem ?? cartItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding to cart: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                }
                return StatusCode(500, new { message = "Internal server error while adding to cart" });
            }
        }

        // PUT: api/CartItems/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, CartItemUpdateDto updateDto)
        {
            try
            {
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(id);
                if (cartItem == null)
                {
                    return NotFound();
                }

                // Get shopping cart
                var shoppingCart = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartItem.ShoppingCartId);
                if (shoppingCart == null)
                {
                    return NotFound();
                }

                // Verify that the user is updating their own cart item
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (!User.IsInRole("Admin") && userId != updateDto.UserId)
                {
                    return Forbid();
                }

                // Verify that the cart item belongs to the user
                if (shoppingCart.UserId != updateDto.UserId)
                {
                    return Forbid();
                }

                // Update quantity
                if (updateDto.Quantity <= 0)
                {
                    // Remove item if quantity is 0 or less
                    _unitOfWork.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = updateDto.Quantity;
                    _unitOfWork.CartItems.Update(cartItem);
                }

                // Update shopping cart last modified date
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating cart item: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating cart item" });
            }
        }

        // DELETE: api/CartItems/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id, [FromQuery] int userId)
        {
            try
            {
                var cartItem = await _unitOfWork.CartItems.GetByIdAsync(id);
                if (cartItem == null)
                {
                    return NotFound();
                }

                // Get shopping cart
                var shoppingCart = await _unitOfWork.ShoppingCarts.GetByIdAsync(cartItem.ShoppingCartId);
                if (shoppingCart == null)
                {
                    return NotFound();
                }

                // Verify that the user is removing their own cart item
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                if (!User.IsInRole("Admin") && currentUserId != userId)
                {
                    return Forbid();
                }

                // Verify that the cart item belongs to the user
                if (shoppingCart.UserId != userId)
                {
                    return Forbid();
                }

                // Remove item
                _unitOfWork.CartItems.Remove(cartItem);

                // Update shopping cart last modified date
                shoppingCart.LastModified = DateTime.Now;
                _unitOfWork.ShoppingCarts.Update(shoppingCart);

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing from cart: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while removing from cart" });
            }
        }
    }

    public class CartItemUpdateDto
    {
        public int Quantity { get; set; }
        public int UserId { get; set; }
    }
    
    public class CartItemCreateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int UserId { get; set; }
    }
} 