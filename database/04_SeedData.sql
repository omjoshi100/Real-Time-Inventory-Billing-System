-- ============================================================================
-- REAL-TIME INVENTORY & BILLING SYSTEM - SEED DATA (SQL SERVER)
-- ============================================================================
USE InventoryBillingDb;
GO

-- 1. SEED ROLES
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES ('Admin', 'Full administrative and financial access');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Cashier')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES ('Cashier', 'Point of sale billing, scanning, and receipt issuance');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Manager')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES ('Manager', 'Inventory tracking, stock adjustments, and reporting');
GO

-- 2. SEED USERS
-- Standard test passwords:
-- admin     -> Admin@123
-- cashier1  -> Cashier@123
-- manager1  -> Manager@123
-- Salt: 'dGVzdHNhbHQxMjM0NTY3OA==' (base64)
-- SHA256(password + salt)
-- Admin@123 + salt hash:    'E13F9F1721532E74B39A0668F17A1689FEBC4571C6793B9FDF7AC2EAA68C4872'
-- Cashier@123 + salt hash:  'A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3'
-- Manager@123 + salt hash:  '48A6F00F299066CA6C88D44E0E87F3B9BBE11A896263B9BBE8B1C88DA6FEAEF1'

DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Admin');
DECLARE @CashierRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Cashier');
DECLARE @ManagerRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'Manager');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
    INSERT INTO dbo.Users (Username, PasswordHash, Salt, FullName, Email, RoleId, IsActive)
    VALUES ('admin', 'E13F9F1721532E74B39A0668F17A1689FEBC4571C6793B9FDF7AC2EAA68C4872', 'dGVzdHNhbHQxMjM0NTY3OA==', 'System Administrator', 'admin@system.local', @AdminRoleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'cashier1')
    INSERT INTO dbo.Users (Username, PasswordHash, Salt, FullName, Email, RoleId, IsActive)
    VALUES ('cashier1', 'A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3', 'dGVzdHNhbHQxMjM0NTY3OA==', 'John Doe (Cashier)', 'cashier@system.local', @CashierRoleId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'manager1')
    INSERT INTO dbo.Users (Username, PasswordHash, Salt, FullName, Email, RoleId, IsActive)
    VALUES ('manager1', '48A6F00F299066CA6C88D44E0E87F3B9BBE11A896263B9BBE8B1C88DA6FEAEF1', 'dGVzdHNhbHQxMjM0NTY3OA==', 'Sarah Connor (Inventory)', 'manager@system.local', @ManagerRoleId, 1);
GO

-- 3. SEED CATEGORIES
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = 'Beverages')
    INSERT INTO dbo.Categories (Name, Description) VALUES ('Beverages', 'Soft drinks, packaged juices, and mineral water');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = 'Snacks & Bakery')
    INSERT INTO dbo.Categories (Name, Description) VALUES ('Snacks & Bakery', 'Biscuits, chips, and confectionery');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = 'Electronics & Gadgets')
    INSERT INTO dbo.Categories (Name, Description) VALUES ('Electronics & Gadgets', 'Accessories, cables, audio, and gadgets');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = 'Dairy & Staples')
    INSERT INTO dbo.Categories (Name, Description) VALUES ('Dairy & Staples', 'Milk, butter, grains, and flour');
GO

-- 4. SEED PRODUCTS (VARYING STOCKS TO DEMO REAL-TIME LOW-STOCK ALERTS)
DECLARE @CatBev INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Beverages');
DECLARE @CatSnk INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Snacks & Bakery');
DECLARE @CatEle INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Electronics & Gadgets');
DECLARE @CatDai INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Dairy & Staples');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'BEV-001')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    VALUES ('BEV-001', '890123450001', 'Artisan Cold Brew Coffee 250ml', 'Nitro infused single origin arabica', @CatBev, 4.50, 2.20, 15, 5, 'can');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'BEV-002')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    -- Intentionally starting near threshold (CurrentStock = 4, Threshold = 5) to test real-time warning!
    VALUES ('BEV-002', '890123450002', 'Organic Green Tea 500ml', 'Freshly brewed matcha blend', @CatBev, 3.25, 1.40, 4, 5, 'bottle');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'SNK-001')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    VALUES ('SNK-001', '890123450003', 'Himalayan Salt Potato Crisps', 'Hand-cooked kettle chips 150g', @CatSnk, 2.75, 1.10, 45, 10, 'bag');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'SNK-002')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    -- Intentionally at threshold (3 remaining, threshold 5)
    VALUES ('SNK-002', '890123450004', 'Dark Chocolate Almond Bar 80g', '72% Belgian dark cocoa', @CatSnk, 3.50, 1.80, 3, 5, 'bar');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'ELE-001')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    VALUES ('ELE-001', '890123450005', 'Fast Charge USB-C Cable (2m)', 'Braided durable 100W PD cable', @CatEle, 14.99, 5.00, 28, 5, 'pcs');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'ELE-002')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    -- Intentionally 2 remaining, threshold 5
    VALUES ('ELE-002', '890123450006', 'Wireless Ergonomic Optical Mouse', 'Dual-mode Bluetooth + 2.4GHz', @CatEle, 29.99, 14.00, 2, 5, 'pcs');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'DAI-001')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    VALUES ('DAI-001', '890123450007', 'Fresh Whole Milk 1L', 'Pasteurized farm fresh whole milk', @CatDai, 1.99, 1.20, 60, 12, 'carton');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = 'DAI-002')
    INSERT INTO dbo.Products (SKU, Barcode, Name, Description, CategoryId, UnitPrice, CostPrice, CurrentStock, LowStockThreshold, Unit)
    VALUES ('DAI-002', '890123450008', 'Cultured Grass-Fed Butter 250g', 'Salted creamy European churned butter', @CatDai, 4.20, 2.50, 18, 6, 'pack');
GO
