-- ============================================================================
-- REAL-TIME INVENTORY & BILLING SYSTEM - DATABASE INDEXES (SQL SERVER)
-- ============================================================================
USE InventoryBillingDb;
GO

-- 1. COVERING INDEX FOR LOW-STOCK DETECTION
-- Explanation: The query `WHERE CurrentStock <= LowStockThreshold AND IsActive = 1`
-- is executed frequently by background workers and real-time alert systems.
-- By including Name, SKU, and UnitPrice in the index leaf pages, SQL Server performs
-- an Index Seek without requiring an expensive Key Lookup to the clustered base table.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_LowStock_Covering' AND object_id = OBJECT_ID('dbo.Products'))
    DROP INDEX IX_Products_LowStock_Covering ON dbo.Products;
GO

CREATE NONCLUSTERED INDEX IX_Products_LowStock_Covering
ON dbo.Products (CurrentStock, LowStockThreshold, IsActive)
INCLUDE (Name, SKU, UnitPrice, CategoryId)
WITH (FILLFACTOR = 90, PAD_INDEX = ON);
GO

-- 2. UNIQUE INDEX ON SKU / BARCODE FOR CASHIER SCANNER LOOKUP
-- Explanation: Cashiers scan barcodes rapidly at checkout. This unique index guarantees
-- O(log N) B-Tree seek latency during item addition to cart.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SKU_Barcode' AND object_id = OBJECT_ID('dbo.Products'))
    DROP INDEX IX_Products_SKU_Barcode ON dbo.Products;
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_Products_SKU_Barcode
ON dbo.Products (SKU)
INCLUDE (ProductId, Name, UnitPrice, CurrentStock, LowStockThreshold, IsActive)
WHERE IsActive = 1;
GO

-- 3. INDEX ON INVOICES FOR SALES ANALYTICS & DASHBOARD KPIS
-- Explanation: The WinForms dashboard queries today's sales and revenue.
-- Ordering by CreatedAt DESC allows the query engine to satisfy top-N feeds
-- and date-range filters directly from the index.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Invoices_CreatedAt_Cashier' AND object_id = OBJECT_ID('dbo.Invoices'))
    DROP INDEX IX_Invoices_CreatedAt_Cashier ON dbo.Invoices;
GO

CREATE NONCLUSTERED INDEX IX_Invoices_CreatedAt_Cashier
ON dbo.Invoices (CreatedAt DESC, CashierId)
INCLUDE (InvoiceNumber, GrandTotal, SubTotal, TaxAmount, AmountPaid, Status);
GO

-- 4. FOREIGN KEY INDEX ON INVOICE ITEMS
-- Explanation: Essential for JOIN performance between Invoices and InvoiceItems
-- when rendering detailed receipt breakdowns.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InvoiceItems_InvoiceId' AND object_id = OBJECT_ID('dbo.InvoiceItems'))
    DROP INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems;
GO

CREATE NONCLUSTERED INDEX IX_InvoiceItems_InvoiceId
ON dbo.InvoiceItems (InvoiceId)
INCLUDE (ProductId, Quantity, UnitPrice, LineTotal);
GO

-- 5. AUDIT TRAIL INDEX ON INVENTORY MOVEMENTS
-- Explanation: Facilitates rapid product ledger inquiries and stock reconciliation.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovements_ProductId_CreatedAt' AND object_id = OBJECT_ID('dbo.InventoryMovements'))
    DROP INDEX IX_InventoryMovements_ProductId_CreatedAt ON dbo.InventoryMovements;
GO

CREATE NONCLUSTERED INDEX IX_InventoryMovements_ProductId_CreatedAt
ON dbo.InventoryMovements (ProductId, CreatedAt DESC)
INCLUDE (MovementType, QuantityChanged, PreviousStock, NewStock, ReferenceId);
GO
