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
    public class InvoicesController : ControllerBase
    {
        private readonly IInventoryRepository _repository;
        private readonly IHubContext<InventoryHub> _hubContext;

        public InvoicesController(IInventoryRepository repository, IHubContext<InventoryHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(new { Message = "Invoice must contain at least one item." });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int cashierId))
            {
                cashierId = 1; // Fallback
            }

            try
            {
                var (invoice, triggeredAlerts) = await _repository.CreateInvoiceWithStockDeductionAsync(cashierId, dto);

                // 1. Broadcast Invoice to all connected screens (Admin live sales ticker & terminals)
                await _hubContext.Clients.All.SendAsync(SignalREvents.ReceiveInvoiceCreated, invoice);

                // 2. Broadcast Low Stock Alerts if any item dropped at or below threshold
                foreach (var alert in triggeredAlerts)
                {
                    await _hubContext.Clients.All.SendAsync(SignalREvents.ReceiveLowStockAlert, alert);
                }

                // 3. Broadcast real-time inventory level adjustments for each sold item
                foreach (var item in dto.Items)
                {
                    var updatedProd = await _repository.GetProductByIdAsync(item.ProductId);
                    if (updatedProd != null)
                    {
                        await _hubContext.Clients.All.SendAsync(
                            SignalREvents.ReceiveInventoryUpdate,
                            updatedProd.ProductId,
                            updatedProd.CurrentStock,
                            updatedProd.IsLowStock
                        );
                    }
                }

                return CreatedAtAction(nameof(GetRecent), new { id = invoice.InvoiceId }, invoice);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred while processing the invoice.", Details = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int limit = 50)
        {
            var invoices = await _repository.GetRecentInvoicesAsync(limit);
            return Ok(invoices);
        }
    }
}
