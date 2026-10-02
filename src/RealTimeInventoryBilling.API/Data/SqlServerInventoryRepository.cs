using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RealTimeInventoryBilling.API.Services;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Data
{
    public class SqlServerInventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public SqlServerInventoryRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("SqlServer") 
                ?? "Server=(localdb)\\mssqllocaldb;Database=InventoryBillingDb;Trusted_Connection=True;MultipleActiveResultSets=true;";
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task InitializeDatabaseAsync()
        {
            // Verify connection; schema is deployed via SQL scripts in production
            await Task.CompletedTask;
        }

        public async Task<UserDto?> AuthenticateUserAsync(string username, string password)
        {
            using var conn = CreateConnection();
            var userRecord = await conn.QuerySingleOrDefaultAsync<dynamic>(
                "dbo.sp_AuthenticateUser",
                new { Username = username },
                commandType: CommandType.StoredProcedure
            );

            if (userRecord == null) return null;

            bool isValid = PasswordSecurity.VerifyPassword(password, (string)userRecord.Salt, (string)userRecord.PasswordHash);
            if (!isValid) return null;

            return new UserDto(
                (int)userRecord.UserId,
                (string)userRecord.Username,
                (string)userRecord.FullName,
                (string?)userRecord.Email,
                (string)userRecord.RoleName,
                (bool)userRecord.IsActive,
                DateTime.UtcNow
            );
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            using var conn = CreateConnection();
            return await conn.QueryAsync<CategoryDto>(
                "SELECT CategoryId, Name, Description FROM dbo.Categories WHERE IsActive = 1 ORDER BY Name"
            );
        }

        public async Task<IEnumerable<ProductDto>> GetProductsAsync(string? search = null, int? categoryId = null)
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT 
                    p.ProductId, p.SKU, p.Barcode, p.Name, p.Description,
                    p.CategoryId, c.Name AS CategoryName,
                    p.UnitPrice, p.CostPrice, p.CurrentStock, p.LowStockThreshold,
                    p.Unit, p.IsActive,
                    CASE WHEN p.CurrentStock <= p.LowStockThreshold THEN 1 ELSE 0 END AS IsLowStock
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE p.IsActive = 1
                  AND (@Search IS NULL OR p.Name LIKE '%' + @Search + '%' OR p.SKU LIKE '%' + @Search + '%' OR p.Barcode LIKE '%' + @Search + '%')
                  AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                ORDER BY p.Name";

            return await conn.QueryAsync<ProductDto>(sql, new { Search = search, CategoryId = categoryId });
        }

        public async Task<ProductDto?> GetProductBySkuOrBarcodeAsync(string code)
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT 
                    p.ProductId, p.SKU, p.Barcode, p.Name, p.Description,
                    p.CategoryId, c.Name AS CategoryName,
                    p.UnitPrice, p.CostPrice, p.CurrentStock, p.LowStockThreshold,
                    p.Unit, p.IsActive,
                    CASE WHEN p.CurrentStock <= p.LowStockThreshold THEN 1 ELSE 0 END AS IsLowStock
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE p.IsActive = 1 AND (p.SKU = @Code OR p.Barcode = @Code)";

            return await conn.QuerySingleOrDefaultAsync<ProductDto>(sql, new { Code = code });
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT 
                    p.ProductId, p.SKU, p.Barcode, p.Name, p.Description,
                    p.CategoryId, c.Name AS CategoryName,
                    p.UnitPrice, p.CostPrice, p.CurrentStock, p.LowStockThreshold,
                    p.Unit, p.IsActive,
                    CASE WHEN p.CurrentStock <= p.LowStockThreshold THEN 1 ELSE 0 END AS IsLowStock
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                WHERE p.ProductId = @Id";

            return await conn.QuerySingleOrDefaultAsync<ProductDto>(sql, new { Id = id });
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            using var conn = CreateConnection();
            var sql = @"
                INSERT INTO dbo.Products (
                    SKU, Barcode, Name, Description, CategoryId, 
                    UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit, IsActive
                ) 
                VALUES (
                    @SKU, @Barcode, @Name, @Description, @CategoryId, 
                    @UnitPrice, @CostPrice, @InitialStock, @LowStockThreshold, @Unit, 1
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newId = await conn.QuerySingleAsync<int>(sql, dto);
            return (await GetProductByIdAsync(newId))!;
        }

        public async Task<bool> UpdateProductAsync(UpdateProductDto dto)
        {
            using var conn = CreateConnection();
            var sql = @"
                UPDATE dbo.Products
                SET SKU = @SKU,
                    Barcode = @Barcode,
                    Name = @Name,
                    Description = @Description,
                    CategoryId = @CategoryId,
                    UnitPrice = @UnitPrice,
                    CostPrice = @CostPrice,
                    LowStockThreshold = @LowStockThreshold,
                    Unit = @Unit,
                    IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE ProductId = @ProductId";

            int rows = await conn.ExecuteAsync(sql, dto);
            return rows > 0;
        }

        public async Task<(InvoiceResponseDto Invoice, List<LowStockAlertDto> TriggeredAlerts)> CreateInvoiceWithStockDeductionAsync(int cashierId, InvoiceCreateDto dto)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var itemsTable = new DataTable();
            itemsTable.Columns.Add("ProductId", typeof(int));
            itemsTable.Columns.Add("Quantity", typeof(int));
            itemsTable.Columns.Add("UnitPrice", typeof(decimal));

            foreach (var item in dto.Items)
            {
                itemsTable.Rows.Add(item.ProductId, item.Quantity, item.UnitPrice);
            }

            var parameters = new DynamicParameters();
            parameters.Add("@CashierId", cashierId);
            parameters.Add("@CustomerName", dto.CustomerName);
            parameters.Add("@CustomerPhone", dto.CustomerPhone);
            parameters.Add("@SubTotal", dto.SubTotal);
            parameters.Add("@TaxRate", dto.TaxRate);
            parameters.Add("@TaxAmount", dto.TaxAmount);
            parameters.Add("@DiscountAmount", dto.DiscountAmount);
            parameters.Add("@GrandTotal", dto.GrandTotal);
            parameters.Add("@AmountPaid", dto.AmountPaid);
            parameters.Add("@ChangeAmount", dto.ChangeAmount);
            parameters.Add("@PaymentMethod", dto.PaymentMethod);
            parameters.Add("@Items", itemsTable.AsTableValuedParameter("dbo.InvoiceItemTableType"));
            parameters.Add("@NewInvoiceId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@NewInvoiceNumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

            using var reader = await conn.ExecuteReaderAsync(
                "dbo.sp_CreateInvoiceWithStockDeduction",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var triggeredAlerts = new List<LowStockAlertDto>();
            while (reader.Read())
            {
                int pId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                string sku = reader.GetString(reader.GetOrdinal("SKU"));
                string name = reader.GetString(reader.GetOrdinal("Name"));
                int currentStock = reader.GetInt32(reader.GetOrdinal("CurrentStock"));
                int threshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold"));
                int isLow = reader.GetInt32(reader.GetOrdinal("IsLowStock"));

                if (isLow == 1)
                {
                    triggeredAlerts.Add(new LowStockAlertDto(pId, sku, name, currentStock, threshold, "General", DateTime.UtcNow));
                }
            }
            reader.Close();

            int newInvoiceId = parameters.Get<int>("@NewInvoiceId");
            string newInvoiceNumber = parameters.Get<string>("@NewInvoiceNumber");

            var cashierName = await conn.QuerySingleOrDefaultAsync<string>(
                "SELECT FullName FROM dbo.Users WHERE UserId = @UserId", new { UserId = cashierId }) ?? "Cashier";

            var savedItems = (await conn.QueryAsync<InvoiceItemResponseDto>(
                @"SELECT InvoiceItemId, ProductId, ProductName, ProductSKU, Quantity, UnitPrice, LineTotal 
                  FROM dbo.InvoiceItems WHERE InvoiceId = @InvoiceId",
                new { InvoiceId = newInvoiceId }
            )).ToList();

            var invoice = new InvoiceResponseDto(
                newInvoiceId,
                newInvoiceNumber,
                cashierId,
                cashierName,
                dto.CustomerName,
                dto.CustomerPhone,
                dto.SubTotal,
                dto.TaxRate,
                dto.TaxAmount,
                dto.DiscountAmount,
                dto.GrandTotal,
                dto.AmountPaid,
                dto.ChangeAmount,
                dto.PaymentMethod,
                "Completed",
                DateTime.UtcNow,
                savedItems
            );

            return (invoice, triggeredAlerts);
        }

        public async Task<(int NewStock, bool IsLowStock, string ProductName, string Sku, int Threshold)> AdjustStockAsync(
            int productId, int quantityChange, string movementType, string reason, int userId)
        {
            using var conn = CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@ProductId", productId);
            parameters.Add("@QuantityChange", quantityChange);
            parameters.Add("@MovementType", movementType);
            parameters.Add("@Reason", reason);
            parameters.Add("@UserId", userId);
            parameters.Add("@NewCurrentStock", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = await conn.QuerySingleAsync<dynamic>(
                "dbo.sp_AdjustInventoryStock",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var product = await GetProductByIdAsync(productId);

            return (
                (int)result.CurrentStock,
                (int)result.IsLowStock == 1,
                product?.Name ?? "Product",
                product?.SKU ?? "SKU",
                product?.LowStockThreshold ?? 10
            );
        }

        public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
        {
            using var conn = CreateConnection();
            return await conn.QueryAsync<ProductDto>(
                "dbo.sp_GetLowStockProducts",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            using var conn = CreateConnection();
            return await conn.QuerySingleAsync<DashboardSummaryDto>(
                "dbo.sp_GetAdminDashboardSummary",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetRecentInvoicesAsync(int limit = 50)
        {
            using var conn = CreateConnection();
            var invoices = (await conn.QueryAsync<InvoiceResponseDto>(
                @"SELECT TOP (@Limit)
                    i.InvoiceId, i.InvoiceNumber, i.CashierId, u.FullName AS CashierName,
                    i.CustomerName, i.CustomerPhone, i.SubTotal, i.TaxRate, i.TaxAmount,
                    i.DiscountAmount, i.GrandTotal, i.AmountPaid, i.ChangeAmount,
                    i.PaymentMethod, i.Status, i.CreatedAt
                  FROM dbo.Invoices i
                  INNER JOIN dbo.Users u ON i.CashierId = u.UserId
                  ORDER BY i.CreatedAt DESC",
                new { Limit = limit }
            )).ToList();

            if (!invoices.Any()) return invoices;

            var invoiceIds = invoices.Select(x => x.InvoiceId).ToList();
            var items = (await conn.QueryAsync<InvoiceItemResponseDto>(
                @"SELECT InvoiceItemId, ProductId, ProductName, ProductSKU, Quantity, UnitPrice, LineTotal, InvoiceId
                  FROM dbo.InvoiceItems WHERE InvoiceId IN @Ids",
                new { Ids = invoiceIds }
            )).ToList();

            return invoices;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT u.UserId, u.Username, u.FullName, u.Email, r.RoleName, u.IsActive, u.LastLoginAt
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
                ORDER BY u.Username";
            return await conn.QueryAsync<UserDto>(sql);
        }
    }
}
