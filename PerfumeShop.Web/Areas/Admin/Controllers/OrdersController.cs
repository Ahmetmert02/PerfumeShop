using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    public class OrdersController : BaseAdminController
    {
        private readonly IApiService _apiService;

        public OrdersController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _apiService.GetOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving orders: {ex.Message}";
                return View(Enumerable.Empty<Order>());
            }
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _apiService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving order details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            try
            {
                var result = await _apiService.UpdateOrderStatusAsync(id, status);
                if (result)
                {
                    TempData["SuccessMessage"] = "Order status updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update order status.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating order status: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 