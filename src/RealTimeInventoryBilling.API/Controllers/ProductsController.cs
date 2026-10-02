using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeInventoryBilling.API.Data;
using RealTimeInventoryBilling.Shared.DTOs;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IInventoryRepository _repository;

        public ProductsController(IInventoryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? categoryId)
        {
            var products = await _repository.GetProductsAsync(search, categoryId);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _repository.GetProductByIdAsync(id);
            if (product == null) return NotFound(new { Message = $"Product with ID {id} was not found." });
            return Ok(product);
        }

        [HttpGet("scan/{code}")]
        public async Task<IActionResult> Scan(string code)
        {
            var product = await _repository.GetProductBySkuOrBarcodeAsync(code);
            if (product == null) return NotFound(new { Message = $"Product with code '{code}' was not found." });
            return Ok(product);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SKU) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { Message = "SKU and Name are mandatory." });
            }

            var created = await _repository.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            if (id != dto.ProductId)
            {
                return BadRequest(new { Message = "Product ID mismatch." });
            }

            var success = await _repository.UpdateProductAsync(dto);
            if (!success) return NotFound(new { Message = $"Product with ID {id} was not found or could not be updated." });

            var updated = await _repository.GetProductByIdAsync(id);
            return Ok(updated);
        }
    }
}
