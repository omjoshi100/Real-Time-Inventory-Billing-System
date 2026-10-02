namespace RealTimeInventoryBilling.Shared.Enums
{
    public enum UserRole
    {
        Admin = 1,
        Cashier = 2,
        Manager = 3
    }

    public enum InventoryMovementType
    {
        Sale = 1,
        Purchase = 2,
        Adjustment = 3,
        Return = 4
    }
}
