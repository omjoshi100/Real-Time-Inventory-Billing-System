using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using RealTimeInventoryBilling.API.Services;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Data
{
    public class SqliteInventoryRepository : IInventoryRepository
    {
        private readonly string _connectionString;

        public SqliteInventoryRepository(IConfiguration config)
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "inventory.db");
            _connectionString = config.GetConnectionString("Sqlite") ?? $"Data Source={dbPath}";
        }

        private SqliteConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task InitializeDatabaseAsync()
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            var ddl = @"
            CREATE TABLE IF NOT EXISTS Roles (
                RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
                RoleName TEXT NOT NULL UNIQUE,
                Description TEXT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Users (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Salt TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Email TEXT NULL,
                RoleId INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                LastLoginAt TEXT NULL,
                FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
            );

            CREATE TABLE IF NOT EXISTS Categories (
                CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Description TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Products (
                ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                SKU TEXT NOT NULL UNIQUE,
                Barcode TEXT NULL,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                CategoryId INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                CostPrice REAL NOT NULL,
                CurrentStock INTEGER NOT NULL DEFAULT 0,
                LowStockThreshold INTEGER NOT NULL DEFAULT 10,
                Unit TEXT NOT NULL DEFAULT 'pcs',
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
            );

            CREATE TABLE IF NOT EXISTS Invoices (
                InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceNumber TEXT NOT NULL UNIQUE,
                CashierId INTEGER NOT NULL,
                CustomerName TEXT NOT NULL DEFAULT 'Walk-in Customer',
                CustomerPhone TEXT NULL,
                SubTotal REAL NOT NULL,
                TaxRate REAL NOT NULL DEFAULT 5.00,
                TaxAmount REAL NOT NULL DEFAULT 0.00,
                DiscountAmount REAL NOT NULL DEFAULT 0.00,
                GrandTotal REAL NOT NULL,
                AmountPaid REAL NOT NULL,
                ChangeAmount REAL NOT NULL DEFAULT 0.00,
                PaymentMethod TEXT NOT NULL DEFAULT 'Cash',
                Status TEXT NOT NULL DEFAULT 'Completed',
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CashierId) REFERENCES Users(UserId)
            );

            CREATE TABLE IF NOT EXISTS InvoiceItems (
                InvoiceItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                ProductSKU TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                LineTotal REAL NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE CASCADE,
                FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
            );

            CREATE TABLE IF NOT EXISTS InventoryMovements (
                MovementId INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductId INTEGER NOT NULL,
                MovementType TEXT NOT NULL,
                QuantityChanged INTEGER NOT NULL,
                PreviousStock INTEGER NOT NULL,
                NewStock INTEGER NOT NULL,
                ReferenceType TEXT NULL,
                ReferenceId TEXT NULL,
                Reason TEXT NULL,
                PerformedByUserId INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
                FOREIGN KEY (PerformedByUserId) REFERENCES Users(UserId)
            );

            CREATE INDEX IF NOT EXISTS IX_Products_LowStock ON Products (CurrentStock, LowStockThreshold, IsActive);
            CREATE INDEX IF NOT EXISTS IX_Invoices_CreatedAt ON Invoices (CreatedAt DESC);
            ";

            await conn.ExecuteAsync(ddl);

            // Seed Roles
            var roleCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Roles");
            if (roleCount == 0)
            {
                var now = DateTime.UtcNow.ToString("o");
                await conn.ExecuteAsync(@"
                    INSERT INTO Roles (RoleName, Description, CreatedAt) VALUES 
                    ('Admin', 'Full administrative and financial access', @Now),
                    ('Cashier', 'Point of sale billing, scanning, and receipt issuance', @Now),
                    ('Manager', 'Inventory tracking, stock adjustments, and reporting', @Now)", new { Now = now });
            }

            // Seed Users
            var userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
            if (userCount == 0)
            {
                var salt = "dGVzdHNhbHQxMjM0NTY3OA==";
                var now = DateTime.UtcNow.ToString("o");
                var adminHash = PasswordSecurity.HashPassword("Admin@123", salt);
                var cashierHash = PasswordSecurity.HashPassword("Cashier@123", salt);
                var managerHash = PasswordSecurity.HashPassword("Manager@123", salt);

                await conn.ExecuteAsync(@"
                    INSERT INTO Users (Username, PasswordHash, Salt, FullName, Email, RoleId, IsActive, CreatedAt) VALUES
                    ('admin', @AdminHash, @Salt, 'System Administrator', 'admin@system.local', 1, 1, @Now),
                    ('cashier1', @CashierHash, @Salt, 'John Doe (Cashier)', 'cashier@system.local', 2, 1, @Now),
                    ('manager1', @ManagerHash, @Salt, 'Sarah Connor (Inventory)', 'manager@system.local', 3, 1, @Now)",
                    new { AdminHash = adminHash, CashierHash = cashierHash, ManagerHash = managerHash, Salt = salt, Now = now });
            }

            // Seed Categories
            var catCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Categories");
            if (catCount == 0)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO Categories (Name, Description, IsActive) VALUES 
                    ('Beverages', 'Soft drinks, packaged juices, and coffee', 1),
                    ('Snacks & Bakery', 'Biscuits, chips, and confectionery', 1),
                    ('Electronics & Gadgets', 'Accessories, cables, audio, and gadgets', 1),
                    ('Dairy & Staples', 'Milk, butter, grains, and flour', 1)");
            }

            // Seed Products
            var prodCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
            if (prodCount == 0)
            {
                var now = DateTime.UtcNow.ToString("o");
                await conn.ExecuteAsync(@"
                    INSERT INTO Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit, IsActive, CreatedAt, UpdatedAt) VALUES 
                    ('BEV-001', '890123450001', 'Artisan Cold Brew Coffee 250ml', 'Nitro infused single origin arabica', 1, 4.50, 2.20, 15, 5, 'can', 1, @Now, @Now),
                    ('BEV-002', '890123450002', 'Organic Green Tea 500ml', 'Freshly brewed matcha blend', 1, 3.25, 1.40, 4, 5, 'bottle', 1, @Now, @Now),
                    ('SNK-001', '890123450003', 'Himalayan Salt Potato Crisps', 'Hand-cooked kettle chips 150g', 2, 2.75, 1.10, 45, 10, 'bag', 1, @Now, @Now),
                    ('SNK-002', '890123450004', 'Dark Chocolate Almond Bar 80g', '72% Belgian dark cocoa', 2, 3.50, 1.80, 3, 5, 'bar', 1, @Now, @Now),
                    ('ELE-001', '890123450005', 'Fast Charge USB-C Cable (2m)', 'Braided durable 100W PD cable', 3, 14.99, 5.00, 28, 5, 'pcs', 1, @Now, @Now),
                    ('ELE-002', '890123450006', 'Wireless Ergonomic Optical Mouse', 'Dual-mode Bluetooth + 2.4GHz', 3, 29.99, 14.00, 2, 5, 'pcs', 1, @Now, @Now),
                    ('DAI-001', '890123450007', 'Fresh Whole Milk 1L', 'Pasteurized farm fresh whole milk', 4, 1.99, 1.20, 60, 12, 'carton', 1, @Now, @Now),
                    ('DAI-002', '890123450008', 'Cultured Grass-Fed Butter 250g', 'Salted creamy European churned butter', 4, 4.20, 2.50, 18, 6, 'pack', 1, @Now, @Now)",
                    new { Now = now });
            }
        }

        public async Task<UserDto?> AuthenticateUserAsync(string username, string password)
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT u.UserId, u.Username, u.PasswordHash, u.Salt, u.FullName, u.Email, r.RoleName, u.IsActive
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                WHERE u.Username = @Username AND u.IsActive = 1";

            var user = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Username = username });
            if (user == null) return null;

            bool isValid = PasswordSecurity.VerifyPassword(password, (string)user.Salt, (string)user.PasswordHash);
            if (!isValid) return null;

            return new UserDto(
                (int)(long)user.UserId,
                (string)user.Username,
                (string)user.FullName,
                (string?)user.Email,
                (string)user.RoleName,
                (long)user.IsActive == 1,
                DateTime.UtcNow
            );
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            using var conn = CreateConnection();
            var records = await conn.QueryAsync<dynamic>("SELECT CategoryId, Name, Description FROM Categories WHERE IsActive = 1 ORDER BY Name");
            return records.Select(r => new CategoryDto((int)(long)r.CategoryId, (string)r.Name, (string?)r.Description));
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
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                WHERE p.IsActive = 1
                  AND (@Search IS NULL OR p.Name LIKE '%' || @Search || '%' OR p.SKU LIKE '%' || @Search || '%' OR p.Barcode LIKE '%' || @Search || '%')
                  AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                ORDER BY p.Name";

            var records = await conn.QueryAsync<dynamic>(sql, new { Search = search, CategoryId = categoryId });
            return records.Select(r => new ProductDto(
                (int)(long)r.ProductId,
                (string)r.SKU,
                (string?)r.Barcode,
                (string)r.Name,
                (string?)r.Description,
                (int)(long)r.CategoryId,
                (string)r.CategoryName,
                Convert.ToDecimal(r.UnitPrice),
                Convert.ToDecimal(r.CostPrice),
                (int)(long)r.CurrentStock,
                (int)(long)r.LowStockThreshold,
                (string)r.Unit,
                (long)r.IsActive == 1,
                (long)r.IsLowStock == 1
            ));
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
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                WHERE p.IsActive = 1 AND (p.SKU = @Code OR p.Barcode = @Code)";

            var r = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Code = code });
            if (r == null) return null;

            return new ProductDto(
                (int)(long)r.ProductId,
                (string)r.SKU,
                (string?)r.Barcode,
                (string)r.Name,
                (string?)r.Description,
                (int)(long)r.CategoryId,
                (string)r.CategoryName,
                Convert.ToDecimal(r.UnitPrice),
                Convert.ToDecimal(r.CostPrice),
                (int)(long)r.CurrentStock,
                (int)(long)r.LowStockThreshold,
                (string)r.Unit,
                (long)r.IsActive == 1,
                (long)r.IsLowStock == 1
            );
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
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                WHERE p.ProductId = @Id";

            var r = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
            if (r == null) return null;

            return new ProductDto(
                (int)(long)r.ProductId,
                (string)r.SKU,
                (string?)r.Barcode,
                (string)r.Name,
                (string?)r.Description,
                (int)(long)r.CategoryId,
                (string)r.CategoryName,
                Convert.ToDecimal(r.UnitPrice),
                Convert.ToDecimal(r.CostPrice),
                (int)(long)r.CurrentStock,
                (int)(long)r.LowStockThreshold,
                (string)r.Unit,
                (long)r.IsActive == 1,
                (long)r.IsLowStock == 1
            );
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            using var conn = CreateConnection();
            var now = DateTime.UtcNow.ToString("o");
            var sql = @"
                INSERT INTO Products (
                    SKU, Barcode, Name, Description, CategoryId, 
                    UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit, IsActive, CreatedAt, UpdatedAt
                ) 
                VALUES (
                    @SKU, @Barcode, @Name, @Description, @CategoryId, 
                    @UnitPrice, @CostPrice, @InitialStock, @LowStockThreshold, @Unit, 1, @Now, @Now
                );
                SELECT last_insert_rowid();";

            long newId = await conn.QuerySingleAsync<long>(sql, new {
                dto.SKU, dto.Barcode, dto.Name, dto.Description, dto.CategoryId,
                UnitPrice = (double)dto.UnitPrice, CostPrice = (double)dto.CostPrice,
                dto.InitialStock, dto.LowStockThreshold, dto.Unit, Now = now
            });

            return (await GetProductByIdAsync((int)newId))!;
        }

        public async Task<bool> UpdateProductAsync(UpdateProductDto dto)
        {
            using var conn = CreateConnection();
            var now = DateTime.UtcNow.ToString("o");
            var sql = @"
                UPDATE Products
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
                    UpdatedAt = @Now
                WHERE ProductId = @ProductId";

            int rows = await conn.ExecuteAsync(sql, new {
                dto.SKU, dto.Barcode, dto.Name, dto.Description, dto.CategoryId,
                UnitPrice = (double)dto.UnitPrice, CostPrice = (double)dto.CostPrice,
                dto.LowStockThreshold, dto.Unit, IsActive = dto.IsActive ? 1 : 0,
                Now = now, dto.ProductId
            });
            return rows > 0;
        }

        public async Task<(InvoiceResponseDto Invoice, List<LowStockAlertDto> TriggeredAlerts)> CreateInvoiceWithStockDeductionAsync(int cashierId, InvoiceCreateDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new InvalidOperationException("Invoice items cannot be empty.");

            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // Validate stock for all items
                foreach (var item in dto.Items)
                {
                    var prod = await conn.QuerySingleOrDefaultAsync<dynamic>(
                        "SELECT Name, CurrentStock FROM Products WHERE ProductId = @Id",
                        new { Id = item.ProductId }, transaction: tx);

                    if (prod == null)
                        throw new InvalidOperationException($"Product with ID {item.ProductId} not found.");

                    int currentStock = (int)(long)prod.CurrentStock;
                    if (currentStock < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for '{prod.Name}'. Available: {currentStock}, Requested: {item.Quantity}");
                }

                // Generate Invoice Number
                var todayPrefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
                var maxSeq = await conn.QuerySingleOrDefaultAsync<string>(
                    "SELECT InvoiceNumber FROM Invoices WHERE InvoiceNumber LIKE @Prefix || '%' ORDER BY InvoiceId DESC LIMIT 1",
                    new { Prefix = todayPrefix }, transaction: tx);

                int nextSeq = 1;
                if (!string.IsNullOrEmpty(maxSeq))
                {
                    var parts = maxSeq.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int parsed))
                        nextSeq = parsed + 1;
                }
                var invoiceNumber = $"{todayPrefix}{nextSeq:D4}";
                var now = DateTime.UtcNow.ToString("o");

                // Insert Invoice
                var insertInvoiceSql = @"
                    INSERT INTO Invoices (
                        InvoiceNumber, CashierId, CustomerName, CustomerPhone,
                        SubTotal, TaxRate, TaxAmount, DiscountAmount,
                        GrandTotal, AmountPaid, ChangeAmount, PaymentMethod,
                        Status, CreatedAt
                    )
                    VALUES (
                        @InvoiceNumber, @CashierId, @CustomerName, @CustomerPhone,
                        @SubTotal, @TaxRate, @TaxAmount, @DiscountAmount,
                        @GrandTotal, @AmountPaid, @ChangeAmount, @PaymentMethod,
                        'Completed', @CreatedAt
                    );
                    SELECT last_insert_rowid();";

                long invoiceId = await conn.QuerySingleAsync<long>(insertInvoiceSql, new {
                    InvoiceNumber = invoiceNumber,
                    CashierId = cashierId,
                    dto.CustomerName,
                    dto.CustomerPhone,
                    SubTotal = (double)dto.SubTotal,
                    TaxRate = (double)dto.TaxRate,
                    TaxAmount = (double)dto.TaxAmount,
                    DiscountAmount = (double)dto.DiscountAmount,
                    GrandTotal = (double)dto.GrandTotal,
                    AmountPaid = (double)dto.AmountPaid,
                    ChangeAmount = (double)dto.ChangeAmount,
                    dto.PaymentMethod,
                    CreatedAt = now
                }, transaction: tx);

                var triggeredAlerts = new List<LowStockAlertDto>();
                var savedItems = new List<InvoiceItemResponseDto>();

                foreach (var item in dto.Items)
                {
                    var prod = await conn.QuerySingleAsync<dynamic>(
                        "SELECT ProductId, SKU, Name, CurrentStock, LowStockThreshold FROM Products WHERE ProductId = @Id",
                        new { Id = item.ProductId }, transaction: tx);

                    string name = (string)prod.Name;
                    string sku = (string)prod.SKU;
                    int oldStock = (int)(long)prod.CurrentStock;
                    int threshold = (int)(long)prod.LowStockThreshold;
                    int newStock = oldStock - item.Quantity;
                    decimal lineTotal = item.Quantity * item.UnitPrice;

                    // Insert Invoice Item
                    var insertItemSql = @"
                        INSERT INTO InvoiceItems (
                            InvoiceId, ProductId, ProductName, ProductSKU, Quantity, UnitPrice, LineTotal
                        )
                        VALUES (@InvoiceId, @ProductId, @ProductName, @ProductSKU, @Quantity, @UnitPrice, @LineTotal);
                        SELECT last_insert_rowid();";

                    long itemId = await conn.QuerySingleAsync<long>(insertItemSql, new {
                        InvoiceId = invoiceId,
                        item.ProductId,
                        ProductName = name,
                        ProductSKU = sku,
                        item.Quantity,
                        UnitPrice = (double)item.UnitPrice,
                        LineTotal = (double)lineTotal
                    }, transaction: tx);

                    savedItems.Add(new InvoiceItemResponseDto((int)itemId, item.ProductId, name, sku, item.Quantity, item.UnitPrice, lineTotal));

                    // Deduct stock
                    await conn.ExecuteAsync(
                        "UPDATE Products SET CurrentStock = @NewStock, UpdatedAt = @Now WHERE ProductId = @Id",
                        new { NewStock = newStock, Now = now, Id = item.ProductId }, transaction: tx);

                    // Record Movement
                    await conn.ExecuteAsync(@"
                        INSERT INTO InventoryMovements (
                            ProductId, MovementType, QuantityChanged, PreviousStock, NewStock,
                            ReferenceType, ReferenceId, Reason, PerformedByUserId, CreatedAt
                        )
                        VALUES (
                            @ProductId, 'Sale', @QuantityChanged, @PreviousStock, @NewStock,
                            'Invoice', @ReferenceId, @Reason, @CashierId, @CreatedAt
                        )", new {
                            item.ProductId,
                            QuantityChanged = -item.Quantity,
                            PreviousStock = oldStock,
                            NewStock = newStock,
                            ReferenceId = invoiceNumber,
                            Reason = $"Sale in Invoice {invoiceNumber}",
                            CashierId = cashierId,
                            CreatedAt = now
                        }, transaction: tx);

                    if (newStock <= threshold)
                    {
                        triggeredAlerts.Add(new LowStockAlertDto(
                            item.ProductId, sku, name, newStock, threshold, "General", DateTime.UtcNow));
                    }
                }

                tx.Commit();

                var cashierName = await conn.QuerySingleOrDefaultAsync<string>(
                    "SELECT FullName FROM Users WHERE UserId = @Id", new { Id = cashierId }) ?? "Cashier";

                var invoiceResponse = new InvoiceResponseDto(
                    (int)invoiceId,
                    invoiceNumber,
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

                return (invoiceResponse, triggeredAlerts);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<(int NewStock, bool IsLowStock, string ProductName, string Sku, int Threshold)> AdjustStockAsync(
            int productId, int quantityChange, string movementType, string reason, int userId)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                var prod = await conn.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT ProductId, SKU, Name, CurrentStock, LowStockThreshold FROM Products WHERE ProductId = @Id",
                    new { Id = productId }, transaction: tx);

                if (prod == null)
                    throw new InvalidOperationException("Product not found.");

                int oldStock = (int)(long)prod.CurrentStock;
                int threshold = (int)(long)prod.LowStockThreshold;
                int newStock = oldStock + quantityChange;

                if (newStock < 0)
                    throw new InvalidOperationException("Stock adjustment would result in negative inventory.");

                var now = DateTime.UtcNow.ToString("o");

                await conn.ExecuteAsync(
                    "UPDATE Products SET CurrentStock = @NewStock, UpdatedAt = @Now WHERE ProductId = @Id",
                    new { NewStock = newStock, Now = now, Id = productId }, transaction: tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO InventoryMovements (
                        ProductId, MovementType, QuantityChanged, PreviousStock, NewStock,
                        ReferenceType, ReferenceId, Reason, PerformedByUserId, CreatedAt
                    )
                    VALUES (
                        @ProductId, @MovementType, @QuantityChanged, @PreviousStock, @NewStock,
                        'StockAdjustment', @ReferenceId, @Reason, @UserId, @CreatedAt
                    )", new {
                        ProductId = productId,
                        MovementType = movementType,
                        QuantityChanged = quantityChange,
                        PreviousStock = oldStock,
                        NewStock = newStock,
                        ReferenceId = productId.ToString(),
                        Reason = reason,
                        UserId = userId,
                        CreatedAt = now
                    }, transaction: tx);

                tx.Commit();

                return (
                    newStock,
                    newStock <= threshold,
                    (string)prod.Name,
                    (string)prod.SKU,
                    threshold
                );
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT 
                    p.ProductId, p.SKU, p.Barcode, p.Name, p.Description,
                    p.CategoryId, c.Name AS CategoryName,
                    p.UnitPrice, p.CostPrice, p.CurrentStock, p.LowStockThreshold,
                    p.Unit, p.IsActive, 1 AS IsLowStock
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                WHERE p.CurrentStock <= p.LowStockThreshold AND p.IsActive = 1
                ORDER BY p.CurrentStock ASC";

            var records = await conn.QueryAsync<dynamic>(sql);
            return records.Select(r => new ProductDto(
                (int)(long)r.ProductId,
                (string)r.SKU,
                (string?)r.Barcode,
                (string)r.Name,
                (string?)r.Description,
                (int)(long)r.CategoryId,
                (string)r.CategoryName,
                Convert.ToDecimal(r.UnitPrice),
                Convert.ToDecimal(r.CostPrice),
                (int)(long)r.CurrentStock,
                (int)(long)r.LowStockThreshold,
                (string)r.Unit,
                (long)r.IsActive == 1,
                true
            ));
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            using var conn = CreateConnection();
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var rev = await conn.ExecuteScalarAsync<double?>(
                "SELECT SUM(GrandTotal) FROM Invoices WHERE CreatedAt >= @Today", new { Today = today }) ?? 0;
            var invCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE CreatedAt >= @Today", new { Today = today });
            var lowCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE CurrentStock <= LowStockThreshold AND IsActive = 1");
            var prodCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Products WHERE IsActive = 1");
            var valuation = await conn.ExecuteScalarAsync<double?>(
                "SELECT SUM(CurrentStock * CostPrice) FROM Products WHERE IsActive = 1") ?? 0;
            var userCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE IsActive = 1");

            return new DashboardSummaryDto(
                Convert.ToDecimal(rev),
                invCount,
                lowCount,
                prodCount,
                Convert.ToDecimal(valuation),
                userCount
            );
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetRecentInvoicesAsync(int limit = 50)
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT 
                    i.InvoiceId, i.InvoiceNumber, i.CashierId, u.FullName AS CashierName,
                    i.CustomerName, i.CustomerPhone, i.SubTotal, i.TaxRate, i.TaxAmount,
                    i.DiscountAmount, i.GrandTotal, i.AmountPaid, i.ChangeAmount,
                    i.PaymentMethod, i.Status, i.CreatedAt
                FROM Invoices i
                INNER JOIN Users u ON i.CashierId = u.UserId
                ORDER BY i.InvoiceId DESC
                LIMIT @Limit";

            var rows = (await conn.QueryAsync<dynamic>(sql, new { Limit = limit })).ToList();
            var result = new List<InvoiceResponseDto>();

            foreach (var r in rows)
            {
                long invId = (long)r.InvoiceId;
                var itemRows = await conn.QueryAsync<dynamic>(
                    "SELECT InvoiceItemId, ProductId, ProductName, ProductSKU, Quantity, UnitPrice, LineTotal FROM InvoiceItems WHERE InvoiceId = @Id",
                    new { Id = invId });

                var items = itemRows.Select(ir => new InvoiceItemResponseDto(
                    (int)(long)ir.InvoiceItemId,
                    (int)(long)ir.ProductId,
                    (string)ir.ProductName,
                    (string)ir.ProductSKU,
                    (int)(long)ir.Quantity,
                    Convert.ToDecimal(ir.UnitPrice),
                    Convert.ToDecimal(ir.LineTotal)
                )).ToList();

                DateTime.TryParse((string)r.CreatedAt, out DateTime createdDate);

                result.Add(new InvoiceResponseDto(
                    (int)invId,
                    (string)r.InvoiceNumber,
                    (int)(long)r.CashierId,
                    (string)r.CashierName,
                    (string)r.CustomerName,
                    (string?)r.CustomerPhone,
                    Convert.ToDecimal(r.SubTotal),
                    Convert.ToDecimal(r.TaxRate),
                    Convert.ToDecimal(r.TaxAmount),
                    Convert.ToDecimal(r.DiscountAmount),
                    Convert.ToDecimal(r.GrandTotal),
                    Convert.ToDecimal(r.AmountPaid),
                    Convert.ToDecimal(r.ChangeAmount),
                    (string)r.PaymentMethod,
                    (string)r.Status,
                    createdDate,
                    items
                ));
            }

            return result;
        }

        public async Task<IEnumerable<UserDto>> GetUsersAsync()
        {
            using var conn = CreateConnection();
            var sql = @"
                SELECT u.UserId, u.Username, u.FullName, u.Email, r.RoleName, u.IsActive, u.LastLoginAt
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                ORDER BY u.Username";

            var records = await conn.QueryAsync<dynamic>(sql);
            return records.Select(r => new UserDto(
                (int)(long)r.UserId,
                (string)r.Username,
                (string)r.FullName,
                (string?)r.Email,
                (string)r.RoleName,
                (long)r.IsActive == 1,
                r.LastLoginAt != null ? DateTime.Parse((string)r.LastLoginAt) : (DateTime?)null
            ));
        }
    }
}
