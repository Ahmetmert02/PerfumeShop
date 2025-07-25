using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;
using PerfumeShop.API.Models;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Brands
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BrandDto>>> GetBrands()
        {
            try
            {
                var brands = await _unitOfWork.Repository<Brand>().GetAllAsync();
                var brandsDto = brands.Select(brand => new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive
                }).ToList();
                
                return Ok(brandsDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
            }
        }

        // GET: api/Brands/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BrandDto>> GetBrand(int id)
        {
            try
            {
                var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);

                if (brand == null)
                {
                    return NotFound();
                }

                var brandDto = new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive
                };
                
                return Ok(brandDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
            }
        }

        // PUT: api/Brands/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutBrand(int id, BrandDto brandDto)
        {
            if (id != brandDto.Id)
            {
                return BadRequest();
            }

            try
            {
                var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }
                
                // Update properties
                brand.Name = brandDto.Name;
                brand.Description = brandDto.Description;
                brand.LogoUrl = brandDto.LogoUrl;
                brand.IsActive = brandDto.IsActive;
                brand.UpdatedAt = DateTime.Now;
                
                _unitOfWork.Repository<Brand>().Update(brand);
                await _unitOfWork.CompleteAsync();
                
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BrandExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
            }
        }

        // POST: api/Brands
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BrandDto>> PostBrand(CreateBrandDto createBrandDto)
        {
            try
            {
                var brand = new Brand
                {
                    Name = createBrandDto.Name,
                    Description = createBrandDto.Description,
                    LogoUrl = createBrandDto.LogoUrl,
                    IsActive = createBrandDto.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                await _unitOfWork.Repository<Brand>().AddAsync(brand);
                await _unitOfWork.CompleteAsync();

                var brandDto = new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Description = brand.Description,
                    LogoUrl = brand.LogoUrl,
                    IsActive = brand.IsActive
                };

                return CreatedAtAction("GetBrand", new { id = brand.Id }, brandDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
            }
        }

        // DELETE: api/Brands/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            try
            {
                var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
                if (brand == null)
                {
                    return NotFound();
                }

                // Prüfen, ob die Marke von Produkten verwendet wird
                var products = await _unitOfWork.Repository<Product>().FindAsync(
                    spec => spec.BrandId == id
                );

                if (products.Any())
                {
                    return BadRequest("Marke kann nicht gelöscht werden, da sie von Produkten verwendet wird.");
                }

                _unitOfWork.Repository<Brand>().Remove(brand);
                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
            }
        }

        private async Task<bool> BrandExists(int id)
        {
            var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
            return brand != null;
        }
    }
} 