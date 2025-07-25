using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Services;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    public class ProductsController : BaseAdminController
    {
        private readonly IApiService _apiService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(IApiService apiService, IWebHostEnvironment webHostEnvironment)
        {
            _apiService = apiService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _apiService.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving products: {ex.Message}";
                return View(Enumerable.Empty<Product>());
            }
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _apiService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new Product { IsActive = true });
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile ImageFile)
        {
            try
            {
                await PopulateDropdowns();
                
                if (ModelState.IsValid)
                {
                    // If an image file was uploaded, save it
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        string imageUrl = await SaveImageAsync(ImageFile);
                        product.ImageUrl = imageUrl;
                    }
                    
                    Console.WriteLine("ModelState is valid, calling API service...");
                    Console.WriteLine($"Image URL after upload: {product.ImageUrl}");
                    Console.WriteLine($"CategoryId: {product.CategoryId}, BrandId: {product.BrandId}");
                    
                    var result = await _apiService.CreateProductAsync(product);
                    
                    Console.WriteLine($"API result: {result}");
                    
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Product created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "API could not create the product. Check console output for details.";
                    }
                }
                else
                {
                    // Show ModelState errors in console
                    Console.WriteLine("ModelState errors:");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                        }
                    }
                }
                
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating product: {ex.Message}";
                return View(product);
            }
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _apiService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }
                
                await PopulateDropdowns();
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile ImageFile)
        {
            try
            {
                await PopulateDropdowns();
                
                if (id != product.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    // If an image file was uploaded, save it
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        string imageUrl = await SaveImageAsync(ImageFile);
                        product.ImageUrl = imageUrl;
                    }
                    
                    var result = await _apiService.UpdateProductAsync(id, product);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Product updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update product.";
                    }
                }
                
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating product: {ex.Message}";
                return View(product);
            }
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _apiService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _apiService.DeleteProductAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Product deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete product.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                // Get categories and brands for dropdowns
                var categories = await _apiService.GetCategoriesAsync();
                var brands = await _apiService.GetBrandsAsync();
                
                // Die SelectList-Namen müssen mit den Namen in der View übereinstimmen
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                ViewBag.Brands = new SelectList(brands, "Id", "Name");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Dropdown-Daten: {ex.Message}");
                ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>());
                ViewBag.Brands = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Generate unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                
                // Return relative URL
                return "/images/products/" + uniqueFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                throw;
            }
        }
    }
} 