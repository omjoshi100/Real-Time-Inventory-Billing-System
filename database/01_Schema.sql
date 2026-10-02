-- ============================================================================
-- REAL-TIME INVENTORY & BILLING SYSTEM - DATABASE SCHEMA (SQL SERVER)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'InventoryBillingDb')
BEGIN
    CREATE DATABASE InventoryBillingDb;
END
GO

USE InventoryBillingDb;
GO

-- Drop tables in reverse foreign-key dependency order if re-running
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.InventoryMovements', 'U') IS NOT NULL DROP TABLE dbo.InventoryMovements;
IF OBJECT_ID('dbo.InvoiceItems', 'U') IS NOT NULL DROP TABLE dbo.InvoiceItems;
IF OBJECT_ID('dbo.Invoices', 'U') IS NOT NULL DROP TABLE dbo.Invoices;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

-- 1. ROLES TABLE
CREATE TABLE dbo.Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(250) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- 2. USERS TABLE
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Salt NVARCHAR(64) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    LastLoginAt DATETIME2 NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

-- 3. CATEGORIES TABLE
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(250) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 4. PRODUCTS TABLE
CREATE TABLE dbo.Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    SKU NVARCHAR(50) NOT NULL UNIQUE,
    Barcode NVARCHAR(50) NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    CategoryId INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    CostPrice DECIMAL(18,2) NOT NULL CHECK (CostPrice >= 0),
    CurrentStock INT NOT NULL DEFAULT 0 CHECK (CurrentStock >= 0),
    LowStockThreshold INT NOT NULL DEFAULT 10 CHECK (LowStockThreshold >= 0),
    Unit NVARCHAR(20) NOT NULL DEFAULT 'pcs',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
);
GO

-- 5. INVOICES TABLE
CREATE TABLE dbo.Invoices (
    InvoiceId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,
    CashierId INT NOT NULL,
    CustomerName NVARCHAR(100) NOT NULL DEFAULT 'Walk-in Customer',
    CustomerPhone NVARCHAR(20) NULL,
    SubTotal DECIMAL(18,2) NOT NULL CHECK (SubTotal >= 0),
    TaxRate DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    GrandTotal DECIMAL(18,2) NOT NULL CHECK (GrandTotal >= 0),
    AmountPaid DECIMAL(18,2) NOT NULL CHECK (AmountPaid >= 0),
    ChangeAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT 'Cash',
    Status NVARCHAR(30) NOT NULL DEFAULT 'Completed',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Invoices_Users FOREIGN KEY (CashierId) REFERENCES dbo.Users(UserId)
);
GO

-- 6. INVOICE ITEMS TABLE
CREATE TABLE dbo.InvoiceItems (
    InvoiceItemId INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(150) NOT NULL,
    ProductSKU NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0),
    LineTotal DECIMAL(18,2) NOT NULL CHECK (LineTotal >= 0),
    CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId) REFERENCES dbo.Invoices(InvoiceId) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);
GO

-- 7. INVENTORY MOVEMENTS (AUDIT TRAIL & LEDGER)
CREATE TABLE dbo.InventoryMovements (
    MovementId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    MovementType NVARCHAR(30) NOT NULL, -- 'Sale', 'Purchase', 'Adjustment', 'Return'
    QuantityChanged INT NOT NULL,
    PreviousStock INT NOT NULL,
    NewStock INT NOT NULL,
    ReferenceType NVARCHAR(50) NULL,
    ReferenceId NVARCHAR(50) NULL,
    Reason NVARCHAR(250) NULL,
    PerformedByUserId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
    CONSTRAINT FK_InventoryMovements_Users FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users(UserId)
);
GO

-- 8. SYSTEM AUDIT LOGS
CREATE TABLE dbo.AuditLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(50) NULL,
    Details NVARCHAR(MAX) NULL,
    IPAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
