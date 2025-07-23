using Microsoft.AspNetCore.Mvc;
using PerfumeShop.Core.Entities;
using PerfumeShop.Web.Services;

namespace PerfumeShop.Web.Areas.Admin.Controllers
{
    public class BrandsController : BaseAdminController
    {
        private readonly IApiService _apiService;

        public BrandsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Admin/Brands
        public async Task<IActionResult> Index()
        {
            try
            {
                var brands = await _apiService.GetBrandsAsync();
                return View(brands);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Abrufen der Marken: {ex.Message}";
                return View(Enumerable.Empty<Brand>());
            }
        }

        // GET: Admin/Brands/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var brand = await _apiService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Abrufen der Marke: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Brands/Create
        public IActionResult Create()
        {
            return View(new Brand { IsActive = true });
        }

        // POST: Admin/Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Über die API erstellen
                    var result = await _apiService.CreateBrandAsync(brand);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Marke erfolgreich erstellt.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                
                TempData["ErrorMessage"] = "Fehler beim Erstellen der Marke.";
                return View(brand);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Erstellen der Marke: {ex.Message}";
                return View(brand);
            }
        }

        // GET: Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var brand = await _apiService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Abrufen der Marke: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            try
            {
                if (id != brand.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    // Über die API aktualisieren
                    var result = await _apiService.UpdateBrandAsync(id, brand);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Marke erfolgreich aktualisiert.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                
                TempData["ErrorMessage"] = "Fehler beim Aktualisieren der Marke.";
                return View(brand);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Aktualisieren der Marke: {ex.Message}";
                return View(brand);
            }
        }

        // GET: Admin/Brands/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _apiService.GetBrandByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                return View(brand);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Abrufen der Marke: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Über die API löschen
                var result = await _apiService.DeleteBrandAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Marke erfolgreich gelöscht.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Fehler beim Löschen der Marke.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Löschen der Marke: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 