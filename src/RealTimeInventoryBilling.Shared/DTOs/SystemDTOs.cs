using System;
using System.Collections.Generic;

namespace RealTimeInventoryBilling.Shared.DTOs
{
    public record LoginRequestDto(string Username, string Password);

    public record AuthResponseDto(
        string Token,
        int UserId,
        string Username,
        string FullName,
        string RoleName,
        DateTime ExpiresAt
    );

    public record UserDto(
        int UserId,
        string Username,
        string FullName,
        string? Email,
        string RoleName,
        bool IsActive,
        DateTime? LastLoginAt
    );

    public record CategoryDto(
        int CategoryId,
        string Name,
        string? Description
    );

    public record ProductDto(
        int ProductId,
        string SKU,
        string? Barcode,
        string Name,
        string? Description,
        int CategoryId,
        string CategoryName,
        decimal UnitPrice,
        decimal CostPrice,
        int CurrentStock,
        int LowStockThreshold,
        string Unit,
        bool IsActive,
        bool IsLowStock
    );

    public record CreateProductDto(
        string SKU,
        string? Barcode,
        string Name,
        string? Description,
        int CategoryId,
        decimal UnitPrice,
        decimal CostPrice,
        int InitialStock,
        int LowStockThreshold,
        string Unit
    );

    public record UpdateProductDto(
        int ProductId,
        string SKU,
        string? Barcode,
        string Name,
        string? Description,
        int CategoryId,
        decimal UnitPrice,
        decimal CostPrice,
        int LowStockThreshold,
        string Unit,
        bool IsActive
    );

    public record StockAdjustmentDto(
        int ProductId,
        int QuantityChange,
        string MovementType,
        string Reason
    );

    public record LowStockAlertDto(
        int ProductId,
        string SKU,
        string Name,
        int CurrentStock,
        int LowStockThreshold,
        string CategoryName,
        DateTime AlertTime
    );

    public record InvoiceItemCreateDto(
        int ProductId,
        int Quantity,
        decimal UnitPrice
    );

    public record InvoiceCreateDto(
        string CustomerName,
        string? CustomerPhone,
        decimal SubTotal,
        decimal TaxRate,
        decimal TaxAmount,
        decimal DiscountAmount,
        decimal GrandTotal,
        decimal AmountPaid,
        decimal ChangeAmount,
        string PaymentMethod,
        List<InvoiceItemCreateDto> Items
    );

    public record InvoiceItemResponseDto(
        int InvoiceItemId,
        int ProductId,
        string ProductName,
        string ProductSKU,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );

    public record InvoiceResponseDto(
        int InvoiceId,
        string InvoiceNumber,
        int CashierId,
        string CashierName,
        string CustomerName,
        string? CustomerPhone,
        decimal SubTotal,
        decimal TaxRate,
        decimal TaxAmount,
        decimal DiscountAmount,
        decimal GrandTotal,
        decimal AmountPaid,
        decimal ChangeAmount,
        string PaymentMethod,
        string Status,
        DateTime CreatedAt,
        List<InvoiceItemResponseDto> Items
    );

    public record DashboardSummaryDto(
        decimal TodayRevenue,
        int TodayInvoicesCount,
        int LowStockCount,
        int TotalProductsCount,
        decimal TotalInventoryValuation,
        int ActiveUsersCount
    );
}
