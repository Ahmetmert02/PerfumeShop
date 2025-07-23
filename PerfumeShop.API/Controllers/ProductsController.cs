using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeShop.API.Models;
using PerfumeShop.Core.Entities;
using PerfumeShop.Core.Interfaces;

namespace PerfumeShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var productDtos = new List<ProductDto>();

            foreach (var product in products)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

                productDtos.Add(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId,
                    CategoryName = category?.Name,
                    BrandName = brand?.Name
                });
            }

            return productDtos;
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
            var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                CategoryName = category?.Name,
                BrandName = brand?.Name
            };

            return productDto;
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProduct(int id, UpdateProductDto productDto)
        {
            if (id != productDto.Id)
            {
                return BadRequest();
            }

            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Update product properties
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.Stock = productDto.Stock;
            product.ImageUrl = productDto.ImageUrl;
            product.IsActive = productDto.IsActive;
            product.CategoryId = productDto.CategoryId;
            product.BrandId = productDto.BrandId;
            product.UpdatedAt = DateTime.Now;

            _unitOfWork.Products.Update(product);

            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "An error occurred while updating the product." });
            }

            return NoContent();
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> PostProduct(CreateProductDto productDto)
        {
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                Stock = productDto.Stock,
                ImageUrl = productDto.ImageUrl,
                IsActive = true,
                CategoryId = productDto.CategoryId,
                BrandId = productDto.BrandId
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
            var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

            var createdProductDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                CategoryName = category?.Name,
                BrandName = brand?.Name
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, createdProductDto);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.CompleteAsync();

            return NoContent();
        }
    }
} 