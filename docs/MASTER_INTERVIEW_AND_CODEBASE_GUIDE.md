# REAL-TIME INVENTORY & BILLING SYSTEM
## Comprehensive Codebase Encyclopedia & Master Technical Interview Preparation Guide
**Author:** Om Joshi  
**Repository:** [https://github.com/omjoshi100/Real-Time-Inventory-Billing-System](https://github.com/omjoshi100/Real-Time-Inventory-Billing-System)  
**Target Roles:** Junior/Associate .NET Software Engineer, Full Stack C# Developer, Backend Engineer  
**Frameworks & Tech Stack:** C# 12 / .NET 10, ASP.NET Core Web API, SignalR (WebSockets), Dapper ORM, Microsoft SQL Server, SQLite, WPF (MVVM), Windows Forms  

---

## TABLE OF CONTENTS
1. [EXECUTIVE SUMMARY & 2-MINUTE ELEVATOR PITCH](#1-executive-summary--2-minute-elevator-pitch)
2. [REPOSITORY ANATOMY: COMPLETE FOLDER DIRECTORY](#2-repository-anatomy-complete-folder-directory)
3. [EXHAUSTIVE FILE-BY-FILE ENCYCLOPEDIA](#3-exhaustive-file-by-file-encyclopedia)
   - [Root Project Files](#31-root-configuration--solution-files)
   - [Database Scripts (database/)](#32-database-scripts-database)
   - [Shared Library (src/RealTimeInventoryBilling.Shared/)](#33-shared-domain-library-srcrealtimeinventorybillingshared)
   - [ASP.NET Core Web API (src/RealTimeInventoryBilling.API/)](#34-backend-web-api-srcrealtimeinventorybillingapi)
   - [WPF POS Terminal (src/RealTimeInventoryBilling.WPF/)](#35-wpf-cashier-pos-terminal-srcrealtimeinventorybillingwpf)
   - [WinForms Admin Console (src/RealTimeInventoryBilling.WinForms/)](#36-winforms-admin-console-srcrealtimeinventorybillingwinforms)
   - [Automation, Installer & Docs](#37-automation-scripts-installer--docs)
4. [DEEP ARCHITECTURAL DATA FLOW & CONCURRENCY SYSTEM](#4-deep-architectural-data-flow--concurrency-system)
5. [MASTER TECHNICAL INTERVIEW Q&A (35 CORE QUESTIONS)](#5-master-technical-interview-qa-35-core-questions)
   - [Category 1: System Design & Architecture](#category-1-system-design--architecture)
   - [Category 2: C# & Modern .NET Core](#category-2-c--modern-net-core)
   - [Category 3: Database, SQL Server & Concurrency](#category-3-database-sql-server--concurrency)
   - [Category 4: Real-Time Networking & SignalR](#category-4-real-time-networking--signalr)
   - [Category 5: Desktop Engineering (WPF MVVM & WinForms)](#category-5-desktop-engineering-wpf-mvvm--winforms)
   - [Category 6: Security, Cryptography & Auth](#category-6-security-cryptography--auth)
6. [INTERVIEWER "GRILLING" SCENARIOS & CODE DEFENSE](#6-interviewer-grilling-scenarios--code-defense)
7. [BEHAVIORAL & CONFIDENCE SCRIPTS FOR FRESHERS](#7-behavioral--confidence-scripts-for-freshers)

---

# 1. EXECUTIVE SUMMARY & 2-MINUTE ELEVATOR PITCH

### The Question: *"Can you introduce yourself and tell me about your project?"*
### Your Word-for-Word Model Answer:
> *"Good morning/afternoon. I have built the **Real-Time Inventory and Billing System**, an enterprise-grade multi-tier retail platform built on **.NET 10 and C# 12**.
>
> In high-volume retail environments—like supermarkets or department stores—a major industry bottleneck is **concurrency and inventory synchronization**. When multiple cashiers ring up the same fast-selling item simultaneously, systems using simple read-then-write logic suffer from race conditions, resulting in overselling and negative inventory. Furthermore, back-office inventory managers usually operate on stale data because standard systems require manual page refreshes or periodic polling.
>
> To solve this, I designed a solution featuring:
> 1. An **ASP.NET Core Web API** utilizing **Dapper** and a custom **SQL Server Stored Procedure** with `WITH (UPDLOCK, ROWLOCK)` locking. This guarantees strictly atomic stock deduction at the database engine level, completely preventing race conditions.
> 2. A **real-time bi-directional messaging pipeline** using **ASP.NET Core SignalR over WebSockets**. The moment a cashier completes a transaction, an event is broadcast to all connected clients in under 50 milliseconds.
> 3. Two specialized frontends:
>    - A **WPF Cashier Terminal** built with strict **MVVM architecture**, reactive barcode search, cart math, and receipt printing.
>    - A high-density **Windows Forms Executive Dashboard** for store managers featuring live KPI metrics, low-stock audio-visual alerts, and an instant stock restocker.
>
> All communication is secured via **JSON Web Tokens (JWT)** with Role-Based Access Control, and passwords are protected using cryptographic **SHA-256 with per-user 16-byte random salts**. The repository contains clean, modular, and unit-testable code adhering to SOLID principles."*

---

# 2. REPOSITORY ANATOMY: COMPLETE FOLDER DIRECTORY

| Folder Path | Purpose & Architectural Role | Why It Exists in the Architecture |
| :--- | :--- | :--- |
| `/` (Root) | Solution root containing build orchestrations, solution descriptors, and global ignore rules. | Organizes the multi-tier projects into a unified build hierarchy. |
| `database/` | Contains all raw, version-controlled SQL scripts for database schema creation, indexing, stored procedures, and initial seed data. | Decouples data definitions from application runtime code; enables automated database provisioning and DBA code reviews. |
| `docs/` | Comprehensive technical documentation, presentations, architecture diagrams, user manuals, and interview guides. | Provides full SDLC transparency, system manuals, and project defense materials for stakeholders. |
| `installer/` | Inno Setup Pascal script configuration for packaging the compiled .NET binaries into an enterprise Windows setup installer (`Setup.exe`). | Allows non-technical retail store managers to install and run the entire suite with a standard Windows wizard. |
| `scripts/` | Automation PowerShell scripts (`build_and_publish.ps1`, `start_demo.ps1`). | Automates CI/CD publishing and provides a single-command demonstration launcher. |
| `src/` | Main source code directory housing the four decoupled .NET projects. | Follows standard Microsoft .NET repo conventions (`src/` vs `tests/` vs `docs/`). |
| `src/RealTimeInventoryBilling.Shared/` | Cross-cutting Class Library referenced by API, WPF, and WinForms. Houses DTOs, Enums, and Constants. | Enforces DRY (Don't Repeat Yourself) principle. Any contract change is immediately compile-checked across all tiers. |
| `src/RealTimeInventoryBilling.API/` | ASP.NET Core Web API acting as the central business logic, persistence, and WebSocket hub. | Centralizes transaction authorization, data persistence, and real-time event dispatching. |
| `src/RealTimeInventoryBilling.API/Controllers/` | HTTP REST endpoints handling inbound API requests from desktop clients. | Acts as the HTTP transport layer; parses requests, validates parameters, and delegates to the repository. |
| `src/RealTimeInventoryBilling.API/Data/` | Data Access Layer (DAL) containing repository interfaces and concrete Dapper implementations. | Implements the Repository Pattern and Dependency Inversion Principle; decouples business logic from SQL Server or SQLite. |
| `src/RealTimeInventoryBilling.API/Hubs/` | SignalR Hub classes managing persistent WebSocket connections and client notification groups. | Handles bidirectional real-time push communication to active desktop clients. |
| `src/RealTimeInventoryBilling.API/Properties/` | Visual Studio runtime profiles and launch settings. | Configures local development ports, environment variables (Development/Production), and IIS Express/Kestrel profiles. |
| `src/RealTimeInventoryBilling.API/Services/` | Core business services (JWT token creation, password hashing, and cryptography). | Encapsulates cross-cutting security algorithms away from controllers and database logic. |
| `src/RealTimeInventoryBilling.WPF/` | Modern Windows Presentation Foundation desktop client tailored for Cashiers and POS checkouts. | Delivers a responsive, hardware-accelerated user experience with MVVM separation and XAML styling. |
| `src/RealTimeInventoryBilling.WPF/Converters/` | XAML `IValueConverter` implementations translating ViewModel data into UI visual states. | Keeps presentation formatting (currency signs, visibility flags, low-stock badge colors) outside of C# ViewModel logic. |
| `src/RealTimeInventoryBilling.WPF/Models/` | Client-side reactive models (e.g., `CartItemModel`) supporting UI data binding. | Represents live checkout state with automatic property-changed calculations. |
| `src/RealTimeInventoryBilling.WPF/Services/` | Client networking services (`ApiClient`, `SignalRClientService`, `ReceiptPrintService`). | Isolates asynchronous HTTP requests, WebSocket connection management, and printing from the ViewModels. |
| `src/RealTimeInventoryBilling.WPF/ViewModels/` | MVVM ViewModel classes implementing `INotifyPropertyChanged` and command relaying. | Maintains UI state, handles user interactions, and coordinates with backend services without touching UI controls. |
| `src/RealTimeInventoryBilling.WPF/Views/` | XAML declarative UI layouts and code-behind files. | Defines visual structure, layout grids, data templates, and user input elements. |
| `src/RealTimeInventoryBilling.WinForms/` | High-density Windows Forms desktop client tailored for Store Managers and Inventory Administrators. | Provides a lightweight, high-performance administrative console for monitoring live sales tickers and inventory metrics. |
| `src/RealTimeInventoryBilling.WinForms/Forms/` | WinForms UI windows (`MainAdminForm`, `LoginForm`, `AdminDialogs`). | Renders dense data grids, KPI summary cards, and administrative modal dialogs. |
| `src/RealTimeInventoryBilling.WinForms/Services/` | Background networking wrappers with thread-safe UI marshaling (`Invoke`/`BeginInvoke`). | Bridges asynchronous background HTTP/SignalR callbacks safely to the WinForms UI thread. |

---

# 3. EXHAUSTIVE FILE-BY-FILE ENCYCLOPEDIA

Below is the definitive, line-by-line file guide. Every single file in the GitHub repository is cataloged with its **Purpose**, **Key Code Components**, **Call Hierarchy**, and **Failure Impact**.

---

## 3.1 ROOT CONFIGURATION & SOLUTION FILES

### 1. `RealTimeInventoryBilling.slnx`
- **Location:** `/RealTimeInventoryBilling.slnx`
- **What it is:** The modern, lightweight XML-based Solution file introduced in recent .NET and Visual Studio releases (replacing the legacy, verbose `.sln` format).
- **Key Contents:** Defines project GUIDs, folder hierarchies, and solution configurations (`Debug|Any CPU`, `Release|Any CPU`) linking all 4 projects: `API`, `Shared`, `WPF`, and `WinForms`.
- **Who calls it:** Visual Studio, Visual Studio Code, JetBrains Rider, and the `dotnet` CLI (`dotnet build RealTimeInventoryBilling.slnx`).
- **If deleted:** The individual projects would still exist, but developers could not open or build the entire ecosystem as a unified solution with one command.

### 2. `.gitignore`
- **Location:** `/.gitignore`
- **What it is:** Git version-control exclusion rules.
- **Key Contents:** Ignores build artifacts (`bin/`, `obj/`), user-specific IDE settings (`.vs/`, `*.user`, `*.suo`), local database files (`*.db`, `*.db-wal`, `*.db-shm`), and temporary build logs.
- **Why it matters:** Prevents multi-megabyte binary executables and sensitive machine-specific state from cluttering the Git commit history.

### 3. `README.md`
- **Location:** `/README.md`
- **What it is:** The primary entry point and visual storefront of the GitHub repository.
- **Key Contents:** System architecture overview, technology badges, credentials table (`Admin` / `Admin@123`, `Cashier1` / `Cashier@123`), quick-start execution commands, database architecture documentation, and REST endpoint specs.
- **Interviewer Value:** Demonstrates professional documentation discipline and pride of craft.

---

## 3.2 DATABASE SCRIPTS (`database/`)

### 4. `database/01_Schema.sql`
- **Location:** `/database/01_Schema.sql`
- **Purpose:** Full DDL (Data Definition Language) script creating the relational database schema in 3rd Normal Form (3NF).
- **Tables Created:**
  - `Users`: Stores `UserId`, `Username`, `PasswordHash`, `PasswordSalt`, `Role`, `FullName`, `IsActive`.
  - `Categories`: Product grouping (`CategoryId`, `Name`, `Description`).
  - `Products`: Product master catalog (`ProductId`, `SKU`, `Name`, `CategoryId`, `UnitPrice`, `CostPrice`, `StockQuantity`, `ReorderThreshold`).
  - `Invoices`: Master billing records (`InvoiceId`, `InvoiceNumber`, `UserId`, `CustomerName`, `CustomerPhone`, `SubTotal`, `TaxAmount`, `DiscountAmount`, `TotalAmount`, `PaymentMethod`, `CreatedAt`).
  - `InvoiceItems`: Transaction detail lines (`InvoiceItemId`, `InvoiceId`, `ProductId`, `Quantity`, `UnitPrice`, `TotalPrice`).
  - `StockLedger`: Immutable double-entry audit log of all physical stock movements (`LedgerId`, `ProductId`, `MovementType`, `QuantityChange`, `RunningBalance`, `ReferenceId`, `Timestamp`).
  - `AuditLogs`: Security and system operation event journal (`LogId`, `UserId`, `Action`, `Timestamp`, `Details`).
- **Interview Highlight:** Point out the `StockLedger` table. Explain that a production billing system should never just overwrite a stock number; every addition or subtraction must have an immutable audit trail.

### 5. `database/02_Indexes.sql`
- **Location:** `/database/02_Indexes.sql`
- **Purpose:** Performance optimization script creating targeted B-Tree indexes.
- **Key Code Highlight:**
  ```sql
  CREATE NONCLUSTERED INDEX IX_Products_LowStock_Covering
  ON Products (StockQuantity, ReorderThreshold)
  INCLUDE (Name, SKU, UnitPrice);
  ```
- **Why this is critical for interviews:** This is a **Covering Index**. Because `Name`, `SKU`, and `UnitPrice` are stored in the leaf nodes via the `INCLUDE` clause, the SQL Server query engine can satisfy the query `WHERE StockQuantity <= ReorderThreshold` purely from the index without executing an expensive **Key Lookup (Bookmark Lookup)** into the clustered table.

### 6. `database/03_StoredProcedures.sql`
- **Location:** `/database/03_StoredProcedures.sql`
- **Purpose:** Transactional business logic running directly inside SQL Server.
- **Key Stored Procedures:**
  - `sp_CreateInvoiceWithStockDeduction`: Accepts an invoice header and a JSON payload of cart items. It executes inside `BEGIN TRANSACTION` with `SET XACT_ABORT ON`.
  - `sp_RestockProduct`: Increments inventory count and appends a `RESTOCK` record into `StockLedger`.
  - `sp_GetDailySalesSummary`: Aggregates total invoices, revenue, tax, and top-selling SKUs for the executive dashboard.
- **The Concurrency Savior:**
  ```sql
  SELECT StockQuantity FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @ProductId;
  ```
  `UPDLOCK` forces a row-level update lock immediately upon reading. If Cashier 1 and Cashier 2 read simultaneously, Cashier 2 is serialized and waits until Cashier 1 commits or rolls back. This makes race conditions mathematically impossible!

### 7. `database/04_SeedData.sql`
- **Location:** `/database/04_SeedData.sql`
- **Purpose:** Inserts initial production-ready data for demonstration.
- **Key Contents:** Pre-hashed admin and cashier accounts with cryptographic salts, 6 core retail categories (Beverages, Dairy, Bakery, Produce, Pantry, Personal Care), and 12 realistic retail products with realistic SKUs and pricing.

---

## 3.3 SHARED DOMAIN LIBRARY (`src/RealTimeInventoryBilling.Shared/`)

### 8. `src/RealTimeInventoryBilling.Shared/RealTimeInventoryBilling.Shared.csproj`
- **Location:** `/src/RealTimeInventoryBilling.Shared/RealTimeInventoryBilling.Shared.csproj`
- **Purpose:** .NET 10 Class Library project file.
- **Why it matters:** Contains zero external UI dependencies. It can be safely compiled and linked into any .NET runtime (Web API, Desktop, Mobile, or CLI).

### 9. `src/RealTimeInventoryBilling.Shared/Constants/SignalREvents.cs`
- **Location:** `/src/RealTimeInventoryBilling.Shared/Constants/SignalREvents.cs`
- **Purpose:** Prevents "magic string" bugs in real-time event subscriptions.
- **Key Constants:**
  - `SignalREvents.StockUpdated`: Broadcast when inventory levels change.
  - `SignalREvents.InvoiceCreated`: Broadcast when a new billing transaction finishes.
  - `SignalREvents.LowStockAlert`: Broadcast when an item dips below its safety threshold.
  - `SignalREvents.HubRoute`: The URL endpoint path (`/hubs/inventory`).
- **If deleted:** Developers would have to hardcode raw string literals across API, WPF, and WinForms, causing silent runtime failures if someone mistyped a string.

### 10. `src/RealTimeInventoryBilling.Shared/DTOs/SystemDTOs.cs`
- **Location:** `/src/RealTimeInventoryBilling.Shared/DTOs/SystemDTOs.cs`
- **Purpose:** Data Transfer Objects defining the immutable data contracts for network serialization.
- **Key DTOs:**
  - `LoginRequest` & `LoginResponse`: Auth credentials and returning JWT token with user roles.
  - `ProductDto`: Lightweight catalog projection sent to POS screens.
  - `CheckoutRequest` & `CheckoutItemDto`: The payload sent when a cashier clicks "Pay".
  - `InvoiceDto` & `InvoiceItemDto`: Full receipt details returned after successful persistence.
  - `DashboardMetricsDto`: Summary metrics (Total Revenue, Active Invoices, Low Stock Count) for managers.
  - `StockUpdateNotificationDto`: Real-time push payload detailing `ProductId`, `NewQuantity`, and `Timestamp`.

### 11. `src/RealTimeInventoryBilling.Shared/Enums/SystemEnums.cs`
- **Location:** `/src/RealTimeInventoryBilling.Shared/Enums/SystemEnums.cs`
- **Purpose:** Strongly typed business domain classifications.
- **Key Enums:**
  - `UserRole`: `Admin = 1`, `Cashier = 2`, `InventoryManager = 3`.
  - `PaymentMethod`: `Cash = 1`, `Card = 2`, `UPI = 3`.
  - `StockMovementType`: `Initial = 1`, `Sale = 2`, `Restock = 3`, `Return = 4`, `Adjustment = 5`.

---

## 3.4 BACKEND WEB API (`src/RealTimeInventoryBilling.API/`)

### 12. `src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.csproj`
- **Location:** `/src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.csproj`
- **Purpose:** ASP.NET Core project file declaring NuGet dependencies.
- **Key Dependencies:**
  - `Dapper`: High-performance micro-ORM.
  - `Microsoft.Data.SqlClient`: Modern Microsoft ADO.NET provider for SQL Server.
  - `Microsoft.Data.Sqlite`: Portable relational database engine for offline mode.
  - `Microsoft.AspNetCore.Authentication.JwtBearer`: Middleware for validating JWT Bearer tokens.
  - `Swashbuckle.AspNetCore`: Swagger OpenAPI interactive documentation.

### 13. `src/RealTimeInventoryBilling.API/Program.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Program.cs`
- **Purpose:** The composition root and application bootstrapper.
- **What it does:**
  1. Configures Kestrel server and loads `appsettings.json`.
  2. Registers the Dependency Injection (DI) container:
     - `IInventoryRepository` $ightarrow$ `SqlServerInventoryRepository` (or `SqliteInventoryRepository` as fallback).
     - `JwtTokenService` and `PasswordSecurity` as singletons/transients.
  3. Configures JWT Bearer authentication with signature verification, issuer validation, and lifetime validation.
  4. Configures CORS (Cross-Origin Resource Sharing) policy.
  5. Maps SignalR Hub route to `/hubs/inventory`.
  6. Automatically verifies and boots the database schema on startup if absent.
- **Interviewer Question:** *"What happens in Program.cs in .NET 10?"*
  - **Answer:** *"It uses the modern Top-Level Statements and Minimal API hosting model. It builds the WebApplicationBuilder, configures services on `builder.Services`, builds the pipeline `app = builder.Build()`, configures HTTP middleware (Routing, CORS, Authentication, Authorization), and calls `app.Run()`."*

### 14. `src/RealTimeInventoryBilling.API/appsettings.json`
- **Location:** `/src/RealTimeInventoryBilling.API/appsettings.json`
- **Purpose:** Production configuration file.
- **Key Contents:**
  - `ConnectionStrings:SqlServer`: Points to SQL Server (`Server=localhost;Database=RealTimeInventoryDb;Trusted_Connection=True;`).
  - `ConnectionStrings:Sqlite`: Local fallback (`Data Source=inventory.db;`).
  - `JwtSettings`: Secret Key (256-bit cryptographic key), Issuer (`RealTimeInventoryBillingAPI`), Audience (`RealTimeInventoryBillingClients`), and Token Expiry (`480` minutes).

### 15. `src/RealTimeInventoryBilling.API/appsettings.Development.json`
- **Location:** `/src/RealTimeInventoryBilling.API/appsettings.Development.json`
- **Purpose:** Development-specific environment overrides (detailed logging for ASP.NET Core and Microsoft hosting).

### 16. `src/RealTimeInventoryBilling.API/Properties/launchSettings.json`
- **Location:** `/src/RealTimeInventoryBilling.API/Properties/launchSettings.json`
- **Purpose:** Local development launch configurations used by Visual Studio and `dotnet run`.
- **Key Contents:**
  - Configures the development web server URLs: `http://localhost:5000` and `https://localhost:5001`.
  - Sets the `ASPNETCORE_ENVIRONMENT` variable to `"Development"`.
  - Enables Swagger UI launch upon startup.
- **If deleted:** The API will default to random ports or Kestrel defaults, which breaks the desktop clients' hardcoded `http://localhost:5000` base URL.

### 17. `src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.http`
- **Location:** `/src/RealTimeInventoryBilling.API/RealTimeInventoryBilling.API.http`
- **Purpose:** In-IDE HTTP scratchpad file for testing API endpoints directly inside Visual Studio or VS Code without needing Postman.

### 18. `src/RealTimeInventoryBilling.API/Data/IInventoryRepository.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Data/IInventoryRepository.cs`
- **Purpose:** The cornerstone interface of the Data Access Layer, implementing the **Repository Pattern** and **Dependency Inversion Principle (SOLID)**.
- **Key Method Contracts:**
  - `Task<User?> GetUserByUsernameAsync(string username)`
  - `Task<IEnumerable<ProductDto>> GetAllProductsAsync()`
  - `Task<ProductDto?> GetProductBySkuAsync(string sku)`
  - `Task<InvoiceDto> CreateInvoiceAsync(CheckoutRequest request, int userId)`
  - `Task<bool> RestockProductAsync(int productId, int quantity, int userId)`
  - `Task<DashboardMetricsDto> GetDashboardMetricsAsync()`
- **Why this is crucial in an interview:**
  - It decouples the Web API controllers from the underlying database technology.
  - Controllers only depend on `IInventoryRepository`, not on SQL Server or SQLite directly.
  - This allows 100% testability using mock repositories (e.g., Moq) in unit test suites.

### 19. `src/RealTimeInventoryBilling.API/Data/SqlServerInventoryRepository.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Data/SqlServerInventoryRepository.cs`
- **Purpose:** High-throughput enterprise implementation of `IInventoryRepository` targeting Microsoft SQL Server using **Dapper**.
- **Key Highlights:**
  - Uses `SqlConnection` with connection pooling.
  - Invokes stored procedures via Dapper: `connection.QueryAsync<ProductDto>("sp_GetActiveProducts", commandType: CommandType.StoredProcedure)`.
  - Formats cart items into JSON to pass directly to `sp_CreateInvoiceWithStockDeduction` for high-speed single-roundtrip execution.

### 20. `src/RealTimeInventoryBilling.API/Data/SqliteInventoryRepository.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Data/SqliteInventoryRepository.cs`
- **Purpose:** Embedded, zero-configuration local relational database implementation of `IInventoryRepository`.
- **Why it was built:** Allows the system to run seamlessly on machines without Microsoft SQL Server installed, guaranteeing zero-friction demonstrations. Uses transactions and SQLite parameterized queries.

### 21. `src/RealTimeInventoryBilling.API/Hubs/InventoryHub.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Hubs/InventoryHub.cs`
- **Purpose:** The ASP.NET Core **SignalR Hub** managing full-duplex WebSocket connections.
- **Key Methods:**
  - `OnConnectedAsync()`: Logs client connection IDs.
  - `JoinGroup(string groupName)`: Segregates clients into groups (e.g., `Cashiers`, `Managers`).
  - Broadcast methods that push notifications to `Clients.All.SendAsync()`.
- **How it interacts:** When `InvoicesController` or `InventoryController` completes a state mutation, they inject `IHubContext<InventoryHub>` to trigger real-time messages to all listening WPF and WinForms clients.

### 22. `src/RealTimeInventoryBilling.API/Services/JwtTokenService.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Services/JwtTokenService.cs`
- **Purpose:** Generates signed, tamper-proof JSON Web Tokens (JWT).
- **Key Logic:**
  - Encodes claims: `ClaimTypes.NameIdentifier` (`UserId`), `ClaimTypes.Name` (`Username`), and `ClaimTypes.Role` (`Role`).
  - Signs the token with a symmetric key using `SecurityAlgorithms.HmacSha256`.
  - Sets token lifetime (8 hours).

### 23. `src/RealTimeInventoryBilling.API/Services/PasswordSecurity.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Services/PasswordSecurity.cs`
- **Purpose:** Cryptographic authentication helper class.
- **Key Methods:**
  - `GenerateSalt()`: Uses `System.Security.Cryptography.RandomNumberGenerator` to produce a 16-byte cryptographically secure pseudorandom salt.
  - `HashPassword(string password, byte[] salt)`: Concatenates password UTF-8 bytes with salt bytes and computes `SHA256.HashData()`.
  - `VerifyPassword(...)`: Computes hash of user input against stored salt and performs a secure comparison.
- **Interview Highlight:** Explain why salt is mandatory: it prevents **Rainbow Table Attacks** where attackers pre-compute hashes of common dictionary passwords.

### 24. `src/RealTimeInventoryBilling.API/Controllers/AuthController.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Controllers/AuthController.cs`
- **Route:** `POST /api/auth/login`
- **Purpose:** Authenticates users and issues JWT tokens. Returns user role, full name, and bearer token.

### 25. `src/RealTimeInventoryBilling.API/Controllers/ProductsController.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Controllers/ProductsController.cs`
- **Routes:** `GET /api/products`, `GET /api/products/search?term={term}`, `GET /api/products/low-stock`.
- **Security:** Protected by `[Authorize]`. Only authenticated cashiers and admins can query the catalog.

### 26. `src/RealTimeInventoryBilling.API/Controllers/InvoicesController.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Controllers/InvoicesController.cs`
- **Routes:** `POST /api/invoices/checkout`, `GET /api/invoices`, `GET /api/invoices/{id}`.
- **Workflow:**
  1. Validates `CheckoutRequest` (cart cannot be empty, quantities must be $> 0$).
  2. Extracts `UserId` from the JWT claims identity.
  3. Calls `_repository.CreateInvoiceAsync(request, userId)`.
  4. On success, calls `_hubContext.Clients.All.SendAsync(SignalREvents.InvoiceCreated, invoice)` and `SignalREvents.StockUpdated`.
  5. Returns `HTTP 201 Created` with full `InvoiceDto`.

### 27. `src/RealTimeInventoryBilling.API/Controllers/InventoryController.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Controllers/InventoryController.cs`
- **Routes:** `POST /api/inventory/restock`, `GET /api/inventory/ledger/{productId}`.
- **Security:** `[Authorize(Roles = "Admin,InventoryManager")]`. Prevents cashiers from unauthorized restocks.

### 28. `src/RealTimeInventoryBilling.API/Controllers/ReportsController.cs`
- **Location:** `/src/RealTimeInventoryBilling.API/Controllers/ReportsController.cs`
- **Routes:** `GET /api/reports/dashboard`, `GET /api/reports/daily-sales`.
- **Purpose:** Serves high-density aggregated metrics to the WinForms manager dashboard.

---

## 3.5 WPF CASHIER POS TERMINAL (`src/RealTimeInventoryBilling.WPF/`)

### 29. `src/RealTimeInventoryBilling.WPF/RealTimeInventoryBilling.WPF.csproj`
- **Location:** `/src/RealTimeInventoryBilling.WPF/RealTimeInventoryBilling.WPF.csproj`
- **Purpose:** Project configuration for .NET 10 Windows Presentation Foundation (`<UseWPF>true</UseWPF>`).
- **Dependencies:** References `RealTimeInventoryBilling.Shared` and `Microsoft.AspNetCore.SignalR.Client`.

### 30. `src/RealTimeInventoryBilling.WPF/App.xaml` & `App.xaml.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/App.xaml`
- **Purpose:** Application-wide resource dictionary. Defines global UI styling: primary colors, button templates, text block typography, and control themes.
- **Code-behind (`App.xaml.cs`):** Manages global unhandled exception trapping to ensure desktop stability during network hiccups.

### 31. `src/RealTimeInventoryBilling.WPF/AssemblyInfo.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/AssemblyInfo.cs`
- **Purpose:** Standard WPF assembly attributes defining `ThemeInfo` mapping.

### 32. `src/RealTimeInventoryBilling.WPF/MainWindow.xaml` & `MainWindow.xaml.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/MainWindow.xaml`
- **Purpose:** The outer visual container (Window Shell) containing the top navigation header, user profile indicator, real-time WebSocket connection status badge, and the `ContentControl` that dynamically renders the active view.

### 33. `src/RealTimeInventoryBilling.WPF/Converters/Converters.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Converters/Converters.cs`
- **Purpose:** Custom XAML value converters:
  - `CurrencyFormatConverter`: Formats numeric values to `$#,##0.00`.
  - `StockLevelToColorConverter`: Turns text Red if `StockQuantity <= ReorderThreshold`, otherwise Green.
  - `BooleanToVisibilityConverter`: Toggles UI element visibility based on boolean flags.

### 34. `src/RealTimeInventoryBilling.WPF/Models/CartItemModel.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Models/CartItemModel.cs`
- **Purpose:** Represents an item currently in the Cashier's shopping cart.
- **Key Code:** Implements `INotifyPropertyChanged`. Whenever `Quantity` or `UnitPrice` is modified, it raises an event for `Subtotal`, instantly updating the total on the cashier screen.

### 35. `src/RealTimeInventoryBilling.WPF/Services/ApiClient.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Services/ApiClient.cs`
- **Purpose:** Typed HTTP client wrapper around `HttpClient`.
- **Responsibilities:**
  - Injects JWT `Authorization: Bearer <token>` into HTTP headers.
  - Serializes and deserializes JSON payloads with `System.Text.Json`.
  - Manages HTTP error status codes and formats descriptive error messages for the user.

### 36. `src/RealTimeInventoryBilling.WPF/Services/SignalRClientService.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Services/SignalRClientService.cs`
- **Purpose:** Manages the persistent WebSocket connection using `HubConnectionBuilder`.
- **Key Features:**
  - Configures `.WithAutomaticReconnect()`: If network drops, it automatically retries with exponential backoff.
  - Subscribes to `SignalREvents.StockUpdated` and raises local C# events for ViewModels.

### 37. `src/RealTimeInventoryBilling.WPF/Services/ReceiptPrintService.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Services/ReceiptPrintService.cs`
- **Purpose:** Formats the completed transaction into a receipt string layout (store header, itemized breakdown, tax/total, barcode footer).

### 38. `src/RealTimeInventoryBilling.WPF/ViewModels/ViewModelBase.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/ViewModels/ViewModelBase.cs`
- **Purpose:** Fundamental MVVM foundation class.
- **Key Components:**
  - Implements `INotifyPropertyChanged` with `SetProperty<T>()` helper using `[CallerMemberName]`.
  - Implements `RelayCommand` and `AsyncRelayCommand` implementing `ICommand` to bind UI button clicks to C# methods without code-behind.

### 39. `src/RealTimeInventoryBilling.WPF/ViewModels/PosViewModels.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/ViewModels/PosViewModels.cs`
- **Purpose:** The core cashier brain.
- **State Managed:** `ObservableCollection<CartItemModel> CartItems`, `SearchTerm`, `SelectedPaymentMethod`, `DiscountPercentage`, `TotalPayable`.
- **Key Actions:** `ScanOrAddProductCommand`, `RemoveCartItemCommand`, `ClearCartCommand`, `ProcessCheckoutCommand`.

### 40. `src/RealTimeInventoryBilling.WPF/ViewModels/AdminViewModels.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/ViewModels/AdminViewModels.cs`
- **Purpose:** ViewModel backing the product management screen. Handles inventory search, threshold editing, and restock dispatching.

### 41. `src/RealTimeInventoryBilling.WPF/Views/LoginWindow.xaml` & `.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Views/LoginWindow.xaml`
- **Purpose:** Cashier authentication modal dialog with password masking.

### 42. `src/RealTimeInventoryBilling.WPF/Views/BillingView.xaml` & `.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Views/BillingView.xaml`
- **Purpose:** The primary Cashier POS terminal UI. Features two-column grid: product search & quick-pick grid on the left, reactive cart and payment totals on the right.

### 43. `src/RealTimeInventoryBilling.WPF/Views/InventoryView.xaml` & `.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Views/InventoryView.xaml`
- **Purpose:** Product catalog view with visual stock badges and restock buttons.

### 44. `src/RealTimeInventoryBilling.WPF/Views/ReceiptWindow.xaml` & `.cs`
- **Location:** `/src/RealTimeInventoryBilling.WPF/Views/ReceiptWindow.xaml`
- **Purpose:** Modal dialog displaying an 80mm thermal receipt preview with "Print" and "Close" commands.

---

## 3.6 WINFORMS ADMIN CONSOLE (`src/RealTimeInventoryBilling.WinForms/`)

### 45. `src/RealTimeInventoryBilling.WinForms/RealTimeInventoryBilling.WinForms.csproj`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/RealTimeInventoryBilling.WinForms.csproj`
- **Purpose:** .NET 10 Windows Forms desktop project file (`<UseWindowsForms>true</UseWindowsForms>`).

### 46. `src/RealTimeInventoryBilling.WinForms/Program.cs`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/Program.cs`
- **Purpose:** WinForms entry point. Configures high-DPI scaling (`ApplicationConfiguration.Initialize()`) and displays the `LoginForm` before loading the `MainAdminForm`.

### 47. `src/RealTimeInventoryBilling.WinForms/Forms/LoginForm.cs`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/Forms/LoginForm.cs`
- **Purpose:** Manager authentication dialog validating administrative credentials against `/api/auth/login`.

### 48. `src/RealTimeInventoryBilling.WinForms/Forms/MainAdminForm.cs`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/Forms/MainAdminForm.cs`
- **Purpose:** The store executive's command center.
- **Key Features:**
  - 4 Real-Time KPI Cards (Today's Revenue, Total Invoices, Low Stock Alerts, Out-of-Stock Items).
  - Live Sales Ticker: Continuously scrolls latest transactions pushed by SignalR.
  - Live Inventory DataGridView: Updates stock levels instantly without user interaction.
  - Low-stock visual flash alerts.

### 49. `src/RealTimeInventoryBilling.WinForms/Forms/AdminDialogs.cs`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/Forms/AdminDialogs.cs`
- **Purpose:** Reusable dialog windows:
  - `RestockDialog`: Allows managers to select a product, enter a restock quantity, and trigger `/api/inventory/restock`.
  - `StockLedgerDialog`: Displays historical stock movement audit logs.

### 50. `src/RealTimeInventoryBilling.WinForms/Services/WinFormsServices.cs`
- **Location:** `/src/RealTimeInventoryBilling.WinForms/Services/WinFormsServices.cs`
- **Purpose:** WinForms HTTP and SignalR background service wrapper.
- **Critical Threading Logic:** Handles **Cross-Thread UI Marshaling**. Background WebSocket threads cannot touch Windows Forms controls directly. This class uses `Control.Invoke` and `Control.BeginInvoke` to marshal updates safely onto the UI thread, preventing `InvalidOperationException: Cross-thread operation not valid`.

---

## 3.7 AUTOMATION SCRIPTS, INSTALLER & DOCS

### 51. `scripts/build_and_publish.ps1`
- **Location:** `/scripts/build_and_publish.ps1`
- **Purpose:** Automated PowerShell CI/CD build script. Compiles the entire solution in `Release` configuration and publishes self-contained binaries to an `artifacts/` folder.

### 52. `scripts/start_demo.ps1`
- **Location:** `/scripts/start_demo.ps1`
- **Purpose:** One-click demo script. Launches the API server in the background, waits for `http://localhost:5000` to be healthy, and simultaneously launches the WPF Cashier terminal and WinForms Manager console side-by-side.

### 53. `installer/setup_script.iss`
- **Location:** `/installer/setup_script.iss`
- **Purpose:** Inno Setup script that compiles all release binaries into an enterprise Windows Setup installer (`Setup.exe`) with desktop shortcuts, Start Menu entries, and uninstaller.

### 54–59. Documentation Files (`docs/`)
- `docs/PROJECT_REPORT.md` / `.doc`: 30-page engineering specification document covering functional/non-functional requirements, ER diagrams, and testing matrices.
- `docs/PRESENTATION.md` / `PRESENTATION.html` / `.doc`: 10-slide executive presentation deck with speaker notes.
- `docs/RealTimeInventoryBilling_UserRunGuide.doc`: Word document guide with exact step-by-step instructions to run, login, test, and troubleshoot the system.
- `docs/MASTER_INTERVIEW_AND_CODEBASE_GUIDE.md` / `.doc`: This master interview prep guide.

---

# 4. DEEP ARCHITECTURAL DATA FLOW & CONCURRENCY SYSTEM

### The End-to-End Checkout Life Cycle (Trace this during your interview!)

```
[Cashier Scans SKU] 
       │
       ▼
[WPF POS: PosViewModels.cs] ──► Validates Cart & Builds CheckoutRequest
       │
       ▼ (HTTP POST /api/invoices/checkout with JWT Bearer Token)
[ASP.NET Core: InvoicesController.cs]
       │
       ▼ (Validates claims, calls repository)
[Data Layer: SqlServerInventoryRepository.cs]
       │
       ▼ (Invokes Stored Procedure)
[SQL Server: sp_CreateInvoiceWithStockDeduction]
       │
       ├─► 1. BEGIN TRANSACTION with SET XACT_ABORT ON
       ├─► 2. SELECT StockQuantity FROM Products WITH (UPDLOCK, ROWLOCK)
       ├─► 3. IF StockQuantity < RequestedQty ──► ROLLBACK & THROW ERROR
       ├─► 4. UPDATE Products SET StockQuantity = StockQuantity - Qty
       ├─► 5. INSERT INTO Invoices & InvoiceItems
       ├─► 6. INSERT INTO StockLedger (MovementType='Sale')
       └─► 7. COMMIT TRANSACTION
       │
       ▼ (Returns full InvoiceDto to Controller)
[ASP.NET Core Hub: InventoryHub.cs]
       │
       ├─► Broadcasts SignalREvents.InvoiceCreated to all connected clients
       └─► Broadcasts SignalREvents.StockUpdated to all connected clients
       │
       ├─────────────────────────────────┬─────────────────────────────────┐
       ▼                                 ▼                                 ▼
[WPF POS Terminal]              [WinForms Admin Console]          [Remote Manager]
- Clears active cart            - Sales Ticker updates live       - Low-stock badge turns RED
- Renders thermal receipt       - KPI Revenue Card increments     - Audio alert plays
```

---

# 5. MASTER TECHNICAL INTERVIEW Q&A (35 CORE QUESTIONS)

---

## CATEGORY 1: SYSTEM DESIGN & ARCHITECTURE

### Q1. Why did you choose a Multi-Tier Architecture instead of building a monolithic desktop application connecting directly to SQL Server?
**Model Answer:**
> *"Connecting a desktop client directly to a database via ADO.NET or EF Core poses severe security and operational risks. 
> 1. **Security:** The database connection string and credentials would have to be distributed to every physical cashier machine, making them vulnerable to reverse engineering.
> 2. **Scalability & Maintenance:** If business rules or database schemas change, every single cashier terminal would require an update. With a multi-tier architecture, the **ASP.NET Core Web API** encapsulates all business rules, authentication, and database access.
> 3. **Real-Time Integration:** A central API allows us to host **SignalR WebSockets**, enabling seamless real-time synchronization across hundreds of distributed terminals."*

### Q2. Why did you develop both a WPF application and a Windows Forms application in the same solution?
**Model Answer:**
> *"This was an intentional architectural decision to demonstrate technology specialization for distinct user personas:
> - **WPF for Cashiers:** Cashiers need a high-speed, modern, visually appealing POS interface. WPF offers resolution-independent vector graphics, powerful data binding, rich animations, and strict MVVM separation ideal for high-volume transactions.
> - **WinForms for Store Managers:** The back-office administrator requires a high-density, low-latency control dashboard. WinForms is exceptionally lightweight, initializes almost instantly, consumes minimal memory, and natively renders dense tabular grids (`DataGridView`) and real-time tickers with zero overhead."*

### Q3. What is the role of the `RealTimeInventoryBilling.Shared` library?
**Model Answer:**
> *"The `Shared` library acts as our **Domain Contract**. It contains DTOs (Data Transfer Objects), Enums, and SignalR event constants. By sharing these contracts across the API, WPF, and WinForms projects, we enforce compile-time type safety across the entire distributed system. If an API contract changes, the desktop clients will fail at compile time rather than crashing at runtime."*

---

## CATEGORY 2: C# & MODERN .NET CORE

### Q4. What are the key advantages of using .NET 10 and C# 12 in this project?
**Model Answer:**
> *"We leveraged:
> 1. **Top-Level Statements & Minimal Hosting:** Drastically reduces boilerplate code in `Program.cs`.
> 2. **C# Records:** Used for immutable DTOs, providing built-in value-based equality, non-destructive mutation (`with`), and concise syntax.
> 3. **High-Performance JSON Serialization:** `System.Text.Json` with source generation minimizes heap allocations.
> 4. **JIT & Runtime Optimizations:** .NET 10 delivers industry-leading throughput and minimal memory footprints."*

### Q5. Explain the difference between `Transient`, `Scoped`, and `Singleton` service lifetimes in ASP.NET Core Dependency Injection. How did you configure them?
**Model Answer:**
> *- **Transient:** Created every time they are requested. Ideal for lightweight, stateless services.
> - **Scoped:** Created once per client HTTP request. Perfect for database contexts and repositories (`IInventoryRepository`).
> - **Singleton:** Created once on application startup and shared across all requests. Used for our `JwtTokenService` and `PasswordSecurity` helpers because they are stateless and computationally thread-safe.*

### Q6. Why do you use `async/await` throughout all database and network operations?
**Model Answer:**
> *"In a high-throughput server, threads are managed by the ThreadPool. If we execute synchronous, blocking I/O calls (like `Thread.Sleep` or synchronous `DbCommand.ExecuteReader`), the calling thread is blocked waiting for the database or network to respond. Under heavy load, this causes **Thread Pool Starvation**. 
> By using `async/await` with `Task`, the thread is released back to the ThreadPool to handle other incoming requests while waiting for the I/O completion port. In WPF and WinForms, `async/await` ensures that the UI thread remains responsive and never freezes during checkouts."*

---

## CATEGORY 3: DATABASE, SQL SERVER & CONCURRENCY

### Q7. Why did you choose Dapper over Entity Framework Core for the database access layer?
**Model Answer:**
> *"While Entity Framework Core is a powerful ORM, high-volume retail checkouts require **microsecond-level latency and absolute control over SQL execution**. 
> 1. **Performance:** Dapper is a lightweight micro-ORM that extends `IDbConnection`. It maps query results directly to C# objects with virtually raw ADO.NET speed.
> 2. **Precise Concurrency Control:** Checkout operations require custom SQL locking hints like `WITH (UPDLOCK, ROWLOCK)` and atomic multi-table stored procedures. Executing stored procedures directly via Dapper eliminates the change tracking, caching overhead, and complex LINQ translation of EF Core."*

### Q8. How does your system prevent race conditions and overselling when two cashiers sell the last item at the exact same instant?
**Model Answer:**
> *"We solve this at the database engine level inside our stored procedure `sp_CreateInvoiceWithStockDeduction`. 
> When reading the product's current stock, we execute:
> `SELECT StockQuantity FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @ProductId;`
> - `ROWLOCK` ensures only the target product row is locked, allowing other products to be sold concurrently.
> - `UPDLOCK` takes an update lock immediately during the read phase. If Cashier 1 and Cashier 2 attempt to read simultaneously, Cashier 2's transaction is blocked until Cashier 1 finishes.
> - Inside the same transaction, if `StockQuantity < RequestedQuantity`, we execute `ROLLBACK` and raise an error. This guarantees ACID compliance and mathematically eliminates overselling."*

### Q9. What is a Covering Index, and how did you implement it in `02_Indexes.sql`?
**Model Answer:**
> *"A Covering Index is a non-clustered index that includes all the columns referenced by a query (both in the `WHERE` filter and the `SELECT` list).
> We created:
> ```sql
> CREATE NONCLUSTERED INDEX IX_Products_LowStock_Covering
> ON Products (StockQuantity, ReorderThreshold)
> INCLUDE (Name, SKU, UnitPrice);
> ```
> When the executive dashboard scans for low stock, the SQL Server query engine retrieves all required fields (`Name`, `SKU`, `UnitPrice`) directly from the B-Tree index pages. It never has to perform an expensive **Bookmark Lookup (Clustered Key Lookup)**, reducing I/O operations from $O(N)$ lookups to a single index seek."*

### Q10. What is the purpose of the `StockLedger` table?
**Model Answer:**
> *"In enterprise retail, directly updating a product's stock number without an audit trail violates financial auditing standards. The `StockLedger` table implements **double-entry inventory bookkeeping**. Every single inventory event (Sale, Restock, Return, Adjustment) writes an immutable ledger entry containing the delta, the running balance, the user who authorized it, and the associated invoice ID."*

---

## CATEGORY 4: REAL-TIME NETWORKING & SIGNALR

### Q11. What is ASP.NET Core SignalR, and what transport protocols does it support?
**Model Answer:**
> *"SignalR is an open-source real-time communication library that enables server code to push content to connected clients instantly. It abstracts the underlying transport protocol and negotiates the best available option in this order:
> 1. **WebSockets (RFC 6455):** Full-duplex, persistent TCP connection with minimal frame overhead (chosen default).
> 2. **Server-Sent Events (SSE):** Persistent HTTP connection supporting server-to-client streaming.
> 3. **Long Polling:** Fallback mechanism where the client repeatedly opens HTTP requests until the server has data."*

### Q12. How does a client handle connection drops in SignalR?
**Model Answer:**
> *"In our desktop services (`SignalRClientService.cs`), we configure the hub connection using `.WithAutomaticReconnect()`. 
> If the network fluctuates, the client automatically attempts reconnection with an exponential backoff strategy (0s, 2s, 10s, 30s). We also hook into the `Reconnecting` and `Reconnected` events to visually notify the user via a status badge on the desktop UI."*

### Q13. How did you resolve the cross-thread UI problem when receiving SignalR events in WPF and WinForms?
**Model Answer:**
> *"SignalR events arrive on background ThreadPool worker threads. In Windows desktop frameworks, UI elements possess thread affinity and can only be modified by the specific UI Dispatcher thread that created them.
> - **In WPF:** We marshal updates using `Application.Current.Dispatcher.Invoke(() => { ... })`.
> - **In WinForms:** We verify `Control.InvokeRequired`. If true, we call `Control.Invoke(new Action(() => { ... }))`.
> Failing to do this causes a runtime `InvalidOperationException`."*

---

## CATEGORY 5: DESKTOP ENGINEERING (WPF MVVM & WINFORMS)

### Q14. Explain the MVVM (Model-View-ViewModel) design pattern and its advantages in your WPF application.
**Model Answer:**
> *- **Model:** Represents raw business entities and DTOs (e.g., `ProductDto`, `CartItemModel`).
> - **View:** The XAML declarative layout (`BillingView.xaml`). It contains no business logic.
> - **ViewModel:** The state machine and orchestrator (`PosViewModels.cs`). It exposes observable properties and commands (`ICommand`).
> - **Advantages:** Strict separation of concerns, testability (ViewModels can be unit tested without initializing UI controls), and reusability of views."*

### Q15. How does XAML data binding know when a property changes in the ViewModel?
**Model Answer:**
> *"Through the `INotifyPropertyChanged` interface. In our `ViewModelBase`, we implement a `SetProperty<T>()` helper method that utilizes the C# `[CallerMemberName]` attribute. When a setter assigns a new value, it raises the `PropertyChanged` event passing the property name string. The WPF binding engine listens for this event and automatically repaints the corresponding UI element."*

### Q16. What is the difference between `RelayCommand` and standard button click event handlers?
**Model Answer:**
> *"Button click event handlers tie business logic directly to the View's code-behind file (`MainWindow.xaml.cs`), which breaks separation of concerns and prevents automated unit testing. 
> `RelayCommand` is an implementation of `System.Windows.Input.ICommand`. It allows us to bind UI button triggers directly to methods inside the ViewModel via XAML declarative syntax (`Command="{Binding ProcessCheckoutCommand}"`). It also supports `CanExecute()` to automatically enable or disable buttons based on system state (e.g., disabling the 'Checkout' button if the cart is empty)."*

---

## CATEGORY 6: SECURITY, CRYPTOGRAPHY & AUTH

### Q17. How does JSON Web Token (JWT) authentication work in this application?
**Model Answer:**
> *"1. The cashier enters their username and password in the desktop client.
> 2. The client sends an HTTP POST request to `/api/auth/login`.
> 3. The server validates credentials and creates a JWT composed of three parts: **Header**, **Payload** (containing claims like `UserId`, `Username`, and `Role`), and a cryptographic **Signature** computed using HMAC-SHA256 with a private secret key.
> 4. The client caches this token in memory and injects it into every subsequent HTTP request header: `Authorization: Bearer <token>`.
> 5. The ASP.NET Core JWT middleware verifies the signature on every request, populates `HttpContext.User`, and enforces role restrictions (`[Authorize(Roles="Admin")]`)."*

### Q18. How do you store and verify passwords securely?
**Model Answer:**
> *"We never store plain-text passwords. In `PasswordSecurity.cs`:
> 1. When a user is registered, we generate a cryptographically random 16-byte salt using `RandomNumberGenerator`.
> 2. We concatenate the password bytes with the salt and compute a **SHA-256** hash.
> 3. We store both the computed hash and the salt in the `Users` table.
> 4. During login, we retrieve the user's stored salt, hash the incoming password with that salt, and compare the result. This completely neutralizes dictionary and rainbow table attacks."*

---

# 6. INTERVIEWER "GRILLING" SCENARIOS & CODE DEFENSE

### Grilling Scenario 1: *"If your SignalR server restarts or the internet connection drops for 5 minutes, what happens to ongoing billing?"*
**Your Confident Defense:**
> *"Great question! In our system, the **checkout transaction is independent of the WebSocket connection**. The checkout is an HTTP REST POST request to the API. If SignalR drops, the checkout still succeeds atomically inside SQL Server. When the SignalR client reconnects using `.WithAutomaticReconnect()`, it automatically fetches the latest state from the API, re-synchronizing inventory levels. Therefore, checkout operations are never compromised by transient network drops."*

### Grilling Scenario 2: *"Why didn't you use WebSockets for checkouts instead of HTTP POST?"*
**Your Confident Defense:**
> *"WebSockets are designed for low-overhead, real-time message streaming. However, financial transactions require **idempotency, standard HTTP status codes (201 Created, 400 Bad Request, 409 Conflict), and robust request-response guarantees**. Using an HTTP POST for checkout gives us standard REST semantics and clear error payloads, while reserving WebSockets strictly for pushing real-time notifications to listening clients."*

### Grilling Scenario 3: *"What if two cashiers add the last remaining item to their cart at the same time? Won't that cause an issue?"*
**Your Confident Defense:**
> *"Adding an item to a local cart does not reserve physical inventory; it only reflects customer intent. The real reservation happens at the moment of checkout. If Cashier 1 clicks 'Pay' first, the row lock `WITH (UPDLOCK, ROWLOCK)` deducts the stock to 0. When Cashier 2 clicks 'Pay' a few seconds later, the stored procedure detects insufficient stock, rolls back Cashier 2's transaction, and returns a 400 Conflict error explaining: 'Insufficient stock for product X'. Furthermore, the moment Cashier 1 checked out, a SignalR `StockUpdated` event was pushed to Cashier 2's screen, highlighting the item in red before they even clicked pay!"*

---

# 7. BEHAVIORAL & CONFIDENCE SCRIPTS FOR FRESHERS

### 1. "What was the most challenging technical bug you faced while developing this project?"
**Your Script:**
> *"The most challenging issue was handling cross-thread UI updates when receiving SignalR events in the Windows Forms client. 
> Initially, when a cashier checked out on the WPF terminal, the WinForms dashboard threw an `InvalidOperationException: Cross-thread operation not valid` and crashed. 
> After analyzing the thread stack trace, I realized that SignalR dispatches callbacks on background ThreadPool worker threads, whereas Windows Forms controls are bound to the main STA UI thread. 
> I resolved this by wrapping the UI update logic inside a thread-marshaling helper using `control.InvokeRequired` and `control.BeginInvoke()`. This ensured all DataGridView and KPI updates were safely scheduled on the UI message loop. It was a fantastic learning experience in multi-threading and Windows message loops."*

### 2. "If you had two more weeks to work on this system, what would you add?"
**Your Script:**
> *"I would implement three enhancements:
> 1. **Distributed Caching with Redis:** Cache the fast-moving product catalog in Redis to reduce database read pressure by 80%.
> 2. **Message Broker with RabbitMQ:** Offload invoice generation and receipt email dispatching to an asynchronous message queue.
> 3. **Offline Local SQLite Synchronization:** Implement a sync engine allowing cashiers to continue ringing up sales even during a complete network outage, queuing transactions locally and syncing with the central SQL Server once connectivity resumes."*

---
*End of Master Guide. Practice these answers out loud, understand the data flows, and you will excel in your interview!*
