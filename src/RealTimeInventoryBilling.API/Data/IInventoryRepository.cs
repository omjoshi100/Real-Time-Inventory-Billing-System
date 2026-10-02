using RealTimeInventoryBilling.Shared.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Data
{
    public interface IInventoryRepository
    {
        Task InitializeDatabaseAsync();
        Task<UserDto?> AuthenticateUserAsync(string username, string password);
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
        Task<IEnumerable<ProductDto>> GetProductsAsync(string? search = null, int? categoryId = null);
        Task<ProductDto?> GetProductBySkuOrBarcodeAsync(string code);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto dto);
        Task<bool> UpdateProductAsync(UpdateProductDto dto);
        Task<(InvoiceResponseDto Invoice, List<LowStockAlertDto> TriggeredAlerts)> CreateInvoiceWithStockDeductionAsync(int cashierId, InvoiceCreateDto dto);
        Task<(int NewStock, bool IsLowStock, string ProductName, string Sku, int Threshold)> AdjustStockAsync(int productId, int quantityChange, string movementType, string reason, int userId);
        Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<IEnumerable<InvoiceResponseDto>> GetRecentInvoicesAsync(int limit = 50);
        Task<IEnumerable<UserDto>> GetUsersAsync();
    }
}
