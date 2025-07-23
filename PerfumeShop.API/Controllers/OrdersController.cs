using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeShop.API.Models;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using System.Collections.Generic;
using System.Text.Json;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrdersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Orders
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = (int)o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
                ShippingAddress = o.ShippingAddress,
                PaymentMethod = o.PaymentMethod,
                OrderItems = o.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            });
            
            return Ok(orderDtos);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Load the OrderItems for this order
            var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == id);
            order.OrderItems = orderItems.ToList();

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = (int)order.Status,
                TotalAmount = order.TotalAmount,
                UserId = order.UserId,
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                OrderItems = order.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };

            return orderDto;
        }

        // GET: api/Orders/User/5
        [HttpGet("User/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetUserOrders(int userId)
        {
            var orders = await _unitOfWork.Orders.FindAsync(o => o.UserId == userId);
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = (int)o.Status,
                TotalAmount = o.TotalAmount,
                UserId = o.UserId,
                ShippingAddress = o.ShippingAddress,
                PaymentMethod = o.PaymentMethod,
                OrderItems = o.OrderItems?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            });
            
            return Ok(orderDtos);
        }

        // POST: api/Orders
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto orderDto)
        {
            try
            {
                Console.WriteLine($"CreateOrder API called with OrderDto: {JsonSerializer.Serialize(orderDto)}");
                
                // Order validation
                if (orderDto == null)
                {
                    Console.WriteLine("OrderDto is null");
                    return BadRequest("Order cannot be null.");
                }

                if (string.IsNullOrEmpty(orderDto.OrderNumber))
                {
                    Console.WriteLine("Order number is empty");
                    return BadRequest("Order number cannot be empty.");
                }

                if (orderDto.TotalAmount <= 0)
                {
                    Console.WriteLine($"Total amount is invalid: {orderDto.TotalAmount}");
                    return BadRequest("Total amount must be greater than 0.");
                }

                if (orderDto.UserId <= 0)
                {
                    Console.WriteLine($"User ID is invalid: {orderDto.UserId}");
                    return BadRequest("User ID must be provided.");
                }

                if (string.IsNullOrEmpty(orderDto.ShippingAddress))
                {
                    Console.WriteLine("Shipping address is empty");
                    return BadRequest("Shipping address cannot be empty.");
                }

                if (string.IsNullOrEmpty(orderDto.PaymentMethod))
                {
                    Console.WriteLine("Payment method is empty");
                    return BadRequest("Payment method cannot be empty.");
                }

                // Check if the user exists
                var user = await _unitOfWork.Users.GetByIdAsync(orderDto.UserId);
                if (user == null)
                {
                    Console.WriteLine($"User with ID {orderDto.UserId} does not exist");
                    return BadRequest($"User with ID {orderDto.UserId} does not exist.");
                }

                // Map DTO to entity
                var order = new Order
                {
                    OrderNumber = orderDto.OrderNumber,
                    OrderDate = orderDto.OrderDate,
                    Status = (OrderStatus)orderDto.Status,
                    TotalAmount = orderDto.TotalAmount,
                    UserId = orderDto.UserId,
                    ShippingAddress = orderDto.ShippingAddress,
                    PaymentMethod = orderDto.PaymentMethod,
                    CreatedAt = DateTime.Now,
                    OrderItems = orderDto.OrderItems.Select(oi => new OrderItem
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice,
                        CreatedAt = DateTime.Now
                    }).ToList()
                };

                // Create the order
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.CompleteAsync();

                // Get the shopping cart for the user
                var shoppingCarts = await _unitOfWork.ShoppingCarts.FindAsync(sc => sc.UserId == orderDto.UserId);
                var shoppingCart = shoppingCarts.FirstOrDefault();
                if (shoppingCart != null)
                {
                    // Clear the shopping cart
                    var cartItems = await _unitOfWork.CartItems.FindAsync(ci => ci.ShoppingCartId == shoppingCart.Id);
                    foreach (var cartItem in cartItems)
                    {
                        _unitOfWork.CartItems.Remove(cartItem);
                    }
                    await _unitOfWork.CompleteAsync();
                }

                // Map entity back to DTO for response
                var createdOrderDto = new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderDate = order.OrderDate,
                    Status = (int)order.Status,
                    TotalAmount = order.TotalAmount,
                    UserId = order.UserId,
                    ShippingAddress = order.ShippingAddress,
                    PaymentMethod = order.PaymentMethod,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        OrderId = oi.OrderId,
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice
                    }).ToList()
                };

                return CreatedAtAction("GetOrder", new { id = order.Id }, createdOrderDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating order: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Internal server error while creating order" });
            }
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            try
            {
                var existingOrder = await _unitOfWork.Orders.GetByIdAsync(id);
                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Update order properties
                existingOrder.Status = order.Status;
                existingOrder.UpdatedAt = DateTime.Now;

                _unitOfWork.Orders.Update(existingOrder);
                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating order" });
            }
        }

        // PUT: api/Orders/Status/5
        [HttpPut("Status/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateModel statusUpdate)
        {
            try
            {
                var existingOrder = await _unitOfWork.Orders.GetByIdAsync(id);
                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Update order status
                existingOrder.Status = statusUpdate.Status;
                existingOrder.UpdatedAt = DateTime.Now;

                _unitOfWork.Orders.Update(existingOrder);
                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while updating order status" });
            }
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Delete order items first
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == id);
                foreach (var item in orderItems)
                {
                    _unitOfWork.OrderItems.Remove(item);
                }

                // Then delete the order
                _unitOfWork.Orders.Remove(order);
                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting order: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error while deleting order" });
            }
        }
    }

    public class OrderStatusUpdateModel
    {
        public OrderStatus Status { get; set; }
    }
} 