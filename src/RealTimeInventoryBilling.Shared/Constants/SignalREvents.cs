namespace RealTimeInventoryBilling.Shared.Constants
{
    public static class SignalREvents
    {
        public const string ReceiveLowStockAlert = "ReceiveLowStockAlert";
        public const string ReceiveInventoryUpdate = "ReceiveInventoryUpdate";
        public const string ReceiveInvoiceCreated = "ReceiveInvoiceCreated";
    }

    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Cashier = "Cashier";
        public const string Manager = "Manager";
    }
}
