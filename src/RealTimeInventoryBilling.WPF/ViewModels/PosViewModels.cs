using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WPF.Models;
using RealTimeInventoryBilling.WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RealTimeInventoryBilling.WPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly ApiClient _api;
        private string _username = "cashier1";
        private string _password = "Cashier@123";
        private string? _errorMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand QuickFillAdminCommand { get; }
        public ICommand QuickFillCashierCommand { get; }
        public ICommand QuickFillManagerCommand { get; }

        public event Action<AuthResponseDto>? OnLoginSuccess;

        public LoginViewModel(ApiClient api)
        {
            _api = api;
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), () => !IsLoading);

            QuickFillAdminCommand = new RelayCommand(() =>
            {
                Username = "admin";
                Password = "Admin@123";
            });

            QuickFillCashierCommand = new RelayCommand(() =>
            {
                Username = "cashier1";
                Password = "Cashier@123";
            });

            QuickFillManagerCommand = new RelayCommand(() =>
            {
                Username = "manager1";
                Password = "Manager@123";
            });
        }

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var auth = await _api.LoginAsync(Username, Password);
                if (auth != null)
                {
                    OnLoginSuccess?.Invoke(auth);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.Replace("Login failed: ", "");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class BillingViewModel : ViewModelBase
    {
        private readonly ApiClient _api;
        private readonly SignalRClientService _signalR;
        private string _searchQuery = string.Empty;
        private string _barcodeInput = string.Empty;
        private decimal _discountAmount = 0.00m;
        private decimal _amountPaid = 0.00m;
        private string _paymentMethod = "Cash";
        private bool _isProcessing;
        private string? _statusMessage;

        public ObservableCollection<ProductDto> Catalog { get; } = new();
        public ObservableCollection<CartItemModel> Cart { get; } = new();

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = LoadCatalogAsync();
                }
            }
        }

        public string BarcodeInput
        {
            get => _barcodeInput;
            set => SetProperty(ref _barcodeInput, value);
        }

        public decimal TaxRate => 5.0m;

        public decimal SubTotal => Cart.Sum(x => x.LineTotal);
        public decimal TaxAmount => Math.Round(SubTotal * (TaxRate / 100m), 2);

        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, Math.Max(0, value)))
                {
                    RecalculateTotals();
                }
            }
        }

        public decimal GrandTotal => Math.Max(0, SubTotal + TaxAmount - DiscountAmount);

        public decimal AmountPaid
        {
            get => _amountPaid;
            set
            {
                if (SetProperty(ref _amountPaid, Math.Max(0, value)))
                {
                    OnPropertyChanged(nameof(ChangeAmount));
                }
            }
        }

        public decimal ChangeAmount => Math.Max(0, AmountPaid - GrandTotal);

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand ScanBarcodeCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand RemoveCartItemCommand { get; }
        public ICommand IncrementQtyCommand { get; }
        public ICommand DecrementQtyCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand RefreshCatalogCommand { get; }

        public event Action<InvoiceResponseDto>? InvoiceCompleted;

        public BillingViewModel(ApiClient api, SignalRClientService signalR)
        {
            _api = api;
            _signalR = signalR;

            ScanBarcodeCommand = new RelayCommand(async () => await ScanBarcodeAsync());
            AddToCartCommand = new RelayCommand(p => AddToCart(p as ProductDto));
            RemoveCartItemCommand = new RelayCommand(c => RemoveFromCart(c as CartItemModel));
            IncrementQtyCommand = new RelayCommand(c => IncrementQty(c as CartItemModel));
            DecrementQtyCommand = new RelayCommand(c => DecrementQty(c as CartItemModel));
            ClearCartCommand = new RelayCommand(ClearCart);
            CheckoutCommand = new RelayCommand(async () => await ExecuteCheckoutAsync(), () => Cart.Any() && !IsProcessing);
            RefreshCatalogCommand = new RelayCommand(async () => await LoadCatalogAsync());

            // Listen to real-time inventory updates from SignalR
            _signalR.InventoryUpdatedReceived += (productId, newStock, isLow) =>
            {
                var prod = Catalog.FirstOrDefault(p => p.ProductId == productId);
                if (prod != null)
                {
                    int idx = Catalog.IndexOf(prod);
                    Catalog[idx] = prod with { CurrentStock = newStock, IsLowStock = isLow };
                }
            };
        }

        public async Task LoadCatalogAsync()
        {
            try
            {
                var products = await _api.GetProductsAsync(SearchQuery);
                Catalog.Clear();
                foreach (var p in products)
                {
                    Catalog.Add(p);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load catalog: {ex.Message}";
            }
        }

        private async Task ScanBarcodeAsync()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

            var code = BarcodeInput.Trim();
            BarcodeInput = string.Empty;

            try
            {
                var prod = await _api.ScanProductAsync(code);
                if (prod != null)
                {
                    AddToCart(prod);
                    StatusMessage = $"Scanned: {prod.Name}";
                }
                else
                {
                    StatusMessage = $"Product with code '{code}' not found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan error: {ex.Message}";
            }
        }

        public void AddToCart(ProductDto? prod)
        {
            if (prod == null) return;

            if (prod.CurrentStock <= 0)
            {
                MessageBox.Show($"'{prod.Name}' is out of stock!", "Stock Depleted", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = Cart.FirstOrDefault(c => c.ProductId == prod.ProductId);
            if (existing != null)
            {
                if (existing.Quantity + 1 > prod.CurrentStock)
                {
                    MessageBox.Show($"Cannot add more '{prod.Name}'. Available stock is {prod.CurrentStock}.", "Insufficient Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                existing.Quantity++;
            }
            else
            {
                Cart.Add(new CartItemModel
                {
                    ProductId = prod.ProductId,
                    SKU = prod.SKU,
                    ProductName = prod.Name,
                    UnitPrice = prod.UnitPrice,
                    AvailableStock = prod.CurrentStock,
                    Quantity = 1
                });
            }

            RecalculateTotals();
        }

        private void RemoveFromCart(CartItemModel? item)
        {
            if (item != null)
            {
                Cart.Remove(item);
                RecalculateTotals();
            }
        }

        private void IncrementQty(CartItemModel? item)
        {
            if (item == null) return;
            var prod = Catalog.FirstOrDefault(p => p.ProductId == item.ProductId);
            int available = prod?.CurrentStock ?? item.AvailableStock;

            if (item.Quantity + 1 > available)
            {
                MessageBox.Show($"Cannot exceed available stock ({available}).", "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            item.Quantity++;
            RecalculateTotals();
        }

        private void DecrementQty(CartItemModel? item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity--;
                RecalculateTotals();
            }
            else
            {
                RemoveFromCart(item);
            }
        }

        public void ClearCart()
        {
            Cart.Clear();
            AmountPaid = 0;
            DiscountAmount = 0;
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(GrandTotal));
            OnPropertyChanged(nameof(ChangeAmount));
        }

        private async Task ExecuteCheckoutAsync()
        {
            if (!Cart.Any()) return;

            if (AmountPaid < GrandTotal)
            {
                MessageBox.Show($"Tender amount (${AmountPaid:F2}) is less than Grand Total (${GrandTotal:F2})!", "Payment Incomplete", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            StatusMessage = "Processing transaction...";

            var invoiceDto = new InvoiceCreateDto(
                CustomerName: "Walk-in Customer",
                CustomerPhone: null,
                SubTotal: SubTotal,
                TaxRate: TaxRate,
                TaxAmount: TaxAmount,
                DiscountAmount: DiscountAmount,
                GrandTotal: GrandTotal,
                AmountPaid: AmountPaid,
                ChangeAmount: ChangeAmount,
                PaymentMethod: PaymentMethod,
                Items: Cart.Select(c => new InvoiceItemCreateDto(c.ProductId, c.Quantity, c.UnitPrice)).ToList()
            );

            try
            {
                var invoice = await _api.CreateInvoiceAsync(invoiceDto);
                ClearCart();
                StatusMessage = $"Invoice {invoice.InvoiceNumber} created successfully!";
                InvoiceCompleted?.Invoke(invoice);
                await LoadCatalogAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Checkout failed: {ex.Message}", "Transaction Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = $"Transaction failed: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
