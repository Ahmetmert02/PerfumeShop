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
        public async Task<IActionResult> Create([Bind("Name,Description,LogoUrl,IsActive")] Brand brand)
        {
            try
            {
                // Debug-Ausgaben für die übermittelten Daten
                Console.WriteLine("========== BRAND CREATE DEBUGGING ==========");
                Console.WriteLine($"Empfangene Daten: Name={brand.Name}, Description={brand.Description}, LogoUrl={brand.LogoUrl}, IsActive={brand.IsActive}");
                
                // ModelState-Fehler protokollieren
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState ist ungültig:");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"- {state.Key}: {error.ErrorMessage}");
                        }
                    }
                    TempData["ErrorMessage"] = "Es gab Validierungsfehler. Bitte überprüfen Sie die eingegebenen Daten.";
                    return View(brand);
                }
                
                // Sicherstellen, dass alle erforderlichen Felder gesetzt sind
                if (string.IsNullOrEmpty(brand.Name))
                {
                    ModelState.AddModelError("Name", "Der Name der Marke ist erforderlich.");
                    Console.WriteLine("Fehler: Name ist leer");
                    TempData["ErrorMessage"] = "Der Name der Marke ist erforderlich.";
                    return View(brand);
                }
                
                Console.WriteLine("ModelState ist gültig, versuche Marke zu erstellen");
                Console.WriteLine($"Brand-Objekt: {brand.Name}, {brand.Description}, {brand.LogoUrl}, {brand.IsActive}");
                
                // Über die API erstellen
                var result = await _apiService.CreateBrandAsync(brand);
                Console.WriteLine($"API-Ergebnis beim Erstellen der Marke: {result}");
                
                if (result)
                {
                    TempData["SuccessMessage"] = "Marke erfolgreich erstellt.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Wenn die Erstellung fehlgeschlagen ist
                TempData["ErrorMessage"] = "Die API konnte die Marke nicht erstellen. Bitte überprüfen Sie die Konsole für Details.";
                return View(brand);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception beim Erstellen der Marke: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
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