using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Text;

namespace RealTimeInventoryBilling.WPF.Services
{
    public class ReceiptPrintService
    {
        public static string GenerateReceiptText(InvoiceResponseDto invoice)
        {
            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("   REAL-TIME INVENTORY & RETAIL POS     ");
            sb.AppendLine("         CAPSTONE DEMO STORE            ");
            sb.AppendLine("========================================");
            sb.AppendLine($"Invoice No : {invoice.InvoiceNumber}");
            sb.AppendLine($"Date       : {invoice.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Cashier    : {invoice.CashierName}");
            sb.AppendLine($"Customer   : {invoice.CustomerName}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,7} {3,8}", "Item", "Qty", "Price", "Total"));
            sb.AppendLine("----------------------------------------");

            foreach (var item in invoice.Items)
            {
                var name = item.ProductName.Length > 18 ? item.ProductName.Substring(0, 15) + "..." : item.ProductName;
                sb.AppendLine(string.Format("{0,-18} {1,4} {2,7:F2} {3,8:F2}", name, item.Quantity, item.UnitPrice, item.LineTotal));
            }

            sb.AppendLine("----------------------------------------");
            sb.AppendLine(string.Format("{0,-25} ${1,10:F2}", "SubTotal:", invoice.SubTotal));
            sb.AppendLine(string.Format("{0,-25} ${1,10:F2}", $"Tax ({invoice.TaxRate}%):", invoice.TaxAmount));
            if (invoice.DiscountAmount > 0)
            {
                sb.AppendLine(string.Format("{0,-25} -${1,10:F2}", "Discount:", invoice.DiscountAmount));
            }
            sb.AppendLine("========================================");
            sb.AppendLine(string.Format("{0,-25} ${1,10:F2}", "GRAND TOTAL:", invoice.GrandTotal));
            sb.AppendLine(string.Format("{0,-25} ${1,10:F2}", "Amount Tendered:", invoice.AmountPaid));
            sb.AppendLine(string.Format("{0,-25} ${1,10:F2}", "Change Due:", invoice.ChangeAmount));
            sb.AppendLine(string.Format("{0,-25} {1,11}", "Payment Method:", invoice.PaymentMethod));
            sb.AppendLine("========================================");
            sb.AppendLine("     Thank You for Shopping With Us!    ");
            sb.AppendLine("         Powered by ASP.NET Core        ");
            sb.AppendLine("     SignalR, SQL Server & WPF MVVM     ");
            sb.AppendLine("========================================");

            return sb.ToString();
        }
    }
}
