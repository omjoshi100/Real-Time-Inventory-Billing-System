using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RealTimeInventoryBilling.API.Data;
using RealTimeInventoryBilling.API.Hubs;
using RealTimeInventoryBilling.Shared.Constants;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _repository;
        private readonly IHubContext<InventoryHub> _hubContext;

        public InventoryController(IInventoryRepository repository, IHubContext<InventoryHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("adjust")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
            {
                userId = 1;
            }

            try
            {
                var (newStock, isLowStock, prodName, sku, threshold) = await _repository.AdjustStockAsync(
                    dto.ProductId,
                    dto.QuantityChange,
                    dto.MovementType,
                    dto.Reason,
                    userId
                );

                // Broadcast stock update to all connected screens
                await _hubContext.Clients.All.SendAsync(
                    SignalREvents.ReceiveInventoryUpdate,
                    dto.ProductId,
                    newStock,
                    isLowStock
                );

                if (isLowStock)
                {
                    var alert = new LowStockAlertDto(
                        dto.ProductId,
                        sku,
                        prodName,
                        newStock,
                        threshold,
                        "General",
                        DateTime.UtcNow
                    );
                    await _hubContext.Clients.All.SendAsync(SignalREvents.ReceiveLowStockAlert, alert);
                }

                return Ok(new
                {
                    ProductId = dto.ProductId,
                    NewStock = newStock,
                    IsLowStock = isLowStock,
                    Message = "Stock adjusted successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock()
        {
            var lowStockItems = await _repository.GetLowStockProductsAsync();
            return Ok(lowStockItems);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _repository.GetCategoriesAsync();
            return Ok(categories);
        }
    }
}
