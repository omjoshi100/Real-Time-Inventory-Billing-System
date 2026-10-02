-- ============================================================================
-- REAL-TIME INVENTORY & BILLING SYSTEM - STORED PROCEDURES (SQL SERVER)
-- ============================================================================
USE InventoryBillingDb;
GO

-- User-Defined Table Type for Batch Invoice Items
IF TYPE_ID('dbo.InvoiceItemTableType') IS NOT NULL
    DROP TYPE dbo.InvoiceItemTableType;
GO

CREATE TYPE dbo.InvoiceItemTableType AS TABLE (
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);
GO

-- 1. AUTHENTICATE USER
IF OBJECT_ID('dbo.sp_AuthenticateUser', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AuthenticateUser;
GO

CREATE PROCEDURE dbo.sp_AuthenticateUser
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        u.UserId,
        u.Username,
        u.PasswordHash,
        u.Salt,
        u.FullName,
        u.Email,
        r.RoleName,
        u.IsActive
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.Username = @Username AND u.IsActive = 1;
END
GO

-- 2. CREATE INVOICE WITH ACID STOCK DEDUCTION
-- Demonstrates strict transaction control, pessimistic row locking (UPDLOCK, ROWLOCK)
-- to eliminate race conditions, stock validation, and audit recording.
IF OBJECT_ID('dbo.sp_CreateInvoiceWithStockDeduction', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateInvoiceWithStockDeduction;
GO

CREATE PROCEDURE dbo.sp_CreateInvoiceWithStockDeduction
    @CashierId INT,
    @CustomerName NVARCHAR(100) = 'Walk-in Customer',
    @CustomerPhone NVARCHAR(20) = NULL,
    @SubTotal DECIMAL(18,2),
    @TaxRate DECIMAL(5,2),
    @TaxAmount DECIMAL(18,2),
    @DiscountAmount DECIMAL(18,2),
    @GrandTotal DECIMAL(18,2),
    @AmountPaid DECIMAL(18,2),
    @ChangeAmount DECIMAL(18,2),
    @PaymentMethod NVARCHAR(50),
    @Items dbo.InvoiceItemTableType READONLY,
    @NewInvoiceId INT OUTPUT,
    @NewInvoiceNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Step A: Validate that the cart is not empty
        IF NOT EXISTS (SELECT 1 FROM @Items)
        BEGIN
            THROW 50001, 'Invoice items table cannot be empty.', 1;
        END

        -- Step B: Acquire UPDLOCK on all targeted products and verify sufficient stock
        -- This prevents concurrent cashiers from overselling the same item.
        DECLARE @InsufficientItem NVARCHAR(150);
        DECLARE @AvailableStock INT;
        DECLARE @RequestedQty INT;

        SELECT TOP 1
            @InsufficientItem = p.Name,
            @AvailableStock = p.CurrentStock,
            @RequestedQty = i.Quantity
        FROM @Items i
        INNER JOIN dbo.Products p WITH (UPDLOCK, ROWLOCK) ON i.ProductId = p.ProductId
        WHERE p.CurrentStock < i.Quantity;

        IF @InsufficientItem IS NOT NULL
        BEGIN
            DECLARE @ErrMsg NVARCHAR(300) = 
                CONCAT('Insufficient stock for product: "', @InsufficientItem, 
                       '". Available: ', @AvailableStock, ', Requested: ', @RequestedQty);
            THROW 50002, @ErrMsg, 1;
        END

        -- Step C: Generate unique formatted invoice number (e.g. INV-YYYYMMDD-XXXX)
        DECLARE @DatePrefix NVARCHAR(20) = CONCAT('INV-', FORMAT(SYSUTCDATETIME(), 'yyyyMMdd-'));
        DECLARE @NextSeq INT;
        
        SELECT @NextSeq = ISNULL(MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT)), 0) + 1
        FROM dbo.Invoices
        WHERE InvoiceNumber LIKE @DatePrefix + '%';

        SET @NewInvoiceNumber = CONCAT(@DatePrefix, RIGHT('0000' + CAST(@NextSeq AS NVARCHAR(4)), 4));

        -- Step D: Insert Invoice Header
        INSERT INTO dbo.Invoices (
            InvoiceNumber, CashierId, CustomerName, CustomerPhone,
            SubTotal, TaxRate, TaxAmount, DiscountAmount,
            GrandTotal, AmountPaid, ChangeAmount, PaymentMethod,
            Status, CreatedAt
        )
        VALUES (
            @NewInvoiceNumber, @CashierId, @CustomerName, @CustomerPhone,
            @SubTotal, @TaxRate, @TaxAmount, @DiscountAmount,
            @GrandTotal, @AmountPaid, @ChangeAmount, @PaymentMethod,
            'Completed', SYSUTCDATETIME()
        );

        SET @NewInvoiceId = SCOPE_IDENTITY();

        -- Step E: Insert Invoice Items
        INSERT INTO dbo.InvoiceItems (
            InvoiceId, ProductId, ProductName, ProductSKU,
            Quantity, UnitPrice, LineTotal
        )
        SELECT 
            @NewInvoiceId,
            p.ProductId,
            p.Name,
            p.SKU,
            i.Quantity,
            i.UnitPrice,
            (i.Quantity * i.UnitPrice)
        FROM @Items i
        INNER JOIN dbo.Products p ON i.ProductId = p.ProductId;

        -- Step F: Deduct Inventory and Record Movements
        INSERT INTO dbo.InventoryMovements (
            ProductId, MovementType, QuantityChanged,
            PreviousStock, NewStock, ReferenceType, ReferenceId,
            Reason, PerformedByUserId, CreatedAt
        )
        SELECT 
            p.ProductId,
            'Sale',
            -i.Quantity,
            p.CurrentStock,
            (p.CurrentStock - i.Quantity),
            'Invoice',
            @NewInvoiceNumber,
            CONCAT('Sale in Invoice ', @NewInvoiceNumber),
            @CashierId,
            SYSUTCDATETIME()
        FROM @Items i
        INNER JOIN dbo.Products p ON i.ProductId = p.ProductId;

        -- Update Products Stock
        UPDATE p
        SET 
            p.CurrentStock = p.CurrentStock - i.Quantity,
            p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Products p
        INNER JOIN @Items i ON p.ProductId = i.ProductId;

        COMMIT TRANSACTION;

        -- Step G: Return items that are now at or below low stock threshold for real-time alerting
        SELECT 
            p.ProductId,
            p.SKU,
            p.Name,
            p.CurrentStock,
            p.LowStockThreshold,
            CASE WHEN p.CurrentStock <= p.LowStockThreshold THEN 1 ELSE 0 END AS IsLowStock
        FROM dbo.Products p
        INNER JOIN @Items i ON p.ProductId = i.ProductId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 3. ADJUST INVENTORY (RESTOCK / SHRINKAGE / RECONCILIATION)
IF OBJECT_ID('dbo.sp_AdjustInventoryStock', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AdjustInventoryStock;
GO

CREATE PROCEDURE dbo.sp_AdjustInventoryStock
    @ProductId INT,
    @QuantityChange INT, -- Positive for Restock, Negative for Shrinkage/Damage
    @MovementType NVARCHAR(30), -- 'Purchase', 'Adjustment', 'Return'
    @Reason NVARCHAR(250),
    @UserId INT,
    @NewCurrentStock INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @OldStock INT;
        DECLARE @Threshold INT;
        DECLARE @ProductName NVARCHAR(150);

        SELECT 
            @OldStock = CurrentStock,
            @Threshold = LowStockThreshold,
            @ProductName = Name
        FROM dbo.Products WITH (UPDLOCK, ROWLOCK)
        WHERE ProductId = @ProductId;

        IF @OldStock IS NULL
        BEGIN
            THROW 50003, 'Product not found.', 1;
        END

        IF (@OldStock + @QuantityChange) < 0
        BEGIN
            THROW 50004, 'Stock adjustment would result in negative inventory.', 1;
        END

        SET @NewCurrentStock = @OldStock + @QuantityChange;

        UPDATE dbo.Products
        SET 
            CurrentStock = @NewCurrentStock,
            UpdatedAt = SYSUTCDATETIME()
        WHERE ProductId = @ProductId;

        INSERT INTO dbo.InventoryMovements (
            ProductId, MovementType, QuantityChanged,
            PreviousStock, NewStock, ReferenceType, ReferenceId,
            Reason, PerformedByUserId, CreatedAt
        )
        VALUES (
            @ProductId, @MovementType, @QuantityChange,
            @OldStock, @NewCurrentStock, 'StockAdjustment', CAST(@ProductId AS NVARCHAR(50)),
            @Reason, @UserId, SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;

        -- Return status
        SELECT 
            ProductId = @ProductId,
            Name = @ProductName,
            CurrentStock = @NewCurrentStock,
            LowStockThreshold = @Threshold,
            IsLowStock = CASE WHEN @NewCurrentStock <= @Threshold THEN 1 ELSE 0 END;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4. GET LOW STOCK PRODUCTS
IF OBJECT_ID('dbo.sp_GetLowStockProducts', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetLowStockProducts;
GO

CREATE PROCEDURE dbo.sp_GetLowStockProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.ProductId,
        p.SKU,
        p.Name,
        p.CategoryId,
        c.Name AS CategoryName,
        p.UnitPrice,
        p.CurrentStock,
        p.LowStockThreshold,
        p.Unit
    FROM dbo.Products p
    INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
    WHERE p.CurrentStock <= p.LowStockThreshold 
      AND p.IsActive = 1
    ORDER BY p.CurrentStock ASC;
END
GO

-- 5. GET ADMIN DASHBOARD SUMMARY
IF OBJECT_ID('dbo.sp_GetAdminDashboardSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAdminDashboardSummary;
GO

CREATE PROCEDURE dbo.sp_GetAdminDashboardSummary
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TodayStart DATETIME2 = CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIME2);

    SELECT 
        (SELECT ISNULL(SUM(GrandTotal), 0) FROM dbo.Invoices WHERE CreatedAt >= @TodayStart) AS TodayRevenue,
        (SELECT COUNT(*) FROM dbo.Invoices WHERE CreatedAt >= @TodayStart) AS TodayInvoicesCount,
        (SELECT COUNT(*) FROM dbo.Products WHERE CurrentStock <= LowStockThreshold AND IsActive = 1) AS LowStockCount,
        (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1) AS TotalProductsCount,
        (SELECT ISNULL(SUM(CurrentStock * CostPrice), 0) FROM dbo.Products WHERE IsActive = 1) AS TotalInventoryValuation,
        (SELECT COUNT(*) FROM dbo.Users WHERE IsActive = 1) AS ActiveUsersCount;
END
GO
