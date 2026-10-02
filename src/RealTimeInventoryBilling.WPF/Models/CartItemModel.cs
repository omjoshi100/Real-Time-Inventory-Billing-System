using RealTimeInventoryBilling.WPF.ViewModels;

namespace RealTimeInventoryBilling.WPF.Models
{
    public class CartItemModel : ViewModelBase
    {
        private int _quantity;
        private decimal _unitPrice;

        public int ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int AvailableStock { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}
