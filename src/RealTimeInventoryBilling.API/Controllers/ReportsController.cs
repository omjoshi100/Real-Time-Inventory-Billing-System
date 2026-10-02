using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeInventoryBilling.API.Data;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IInventoryRepository _repository;

        public ReportsController(IInventoryRepository repository)
        {
            _repository = repository;
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var summary = await _repository.GetDashboardSummaryAsync();
            return Ok(summary);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _repository.GetUsersAsync();
            return Ok(users);
        }
    }
}
