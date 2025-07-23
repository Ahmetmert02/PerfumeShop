using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    public class CategoriesController : BaseAdminController
    {
        private readonly IApiService _apiService;

        public CategoriesController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _apiService.GetCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving categories: {ex.Message}";
                return View(Enumerable.Empty<Category>());
            }
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _apiService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving category: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View(new Category { IsActive = true });
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _apiService.CreateCategoryAsync(category);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Category created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create category.";
                    }
                }
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating category: {ex.Message}";
                return View(category);
            }
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _apiService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving category: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IsActive")] Category category)
        {
            try
            {
                if (id != category.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    var result = await _apiService.UpdateCategoryAsync(id, category);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Category updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update category.";
                    }
                }
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating category: {ex.Message}";
                return View(category);
            }
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _apiService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving category: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _apiService.DeleteCategoryAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Category deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete category. It may be in use by products.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting category: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 