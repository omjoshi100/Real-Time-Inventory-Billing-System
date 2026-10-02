using Microsoft.AspNetCore.SignalR;
using RealTimeInventoryBilling.Shared.Constants;
using RealTimeInventoryBilling.Shared.DTOs;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.API.Hubs
{
    public class InventoryHub : Hub
    {
        public async Task BroadcastLowStockAlert(LowStockAlertDto alert)
        {
            await Clients.All.SendAsync(SignalREvents.ReceiveLowStockAlert, alert);
        }

        public async Task BroadcastInventoryUpdate(int productId, int newStock, bool isLowStock)
        {
            await Clients.All.SendAsync(SignalREvents.ReceiveInventoryUpdate, productId, newStock, isLowStock);
        }

        public async Task BroadcastInvoiceCreated(InvoiceResponseDto invoice)
        {
            await Clients.All.SendAsync(SignalREvents.ReceiveInvoiceCreated, invoice);
        }
    }
}
