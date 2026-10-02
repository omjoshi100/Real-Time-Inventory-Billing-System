using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RealTimeInventoryBilling.WPF.ViewModels
{
    public class InventoryViewModel : ViewModelBase
    {
        private readonly ApiClient _api;
        private readonly SignalRClientService _signalR;
        private string _searchQuery = string.Empty;
        private bool _isLoading;

        public ObservableCollection<ProductDto> Products { get; } = new();

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _ = LoadProductsAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int LowStockCount => Products.Count(p => p.IsLowStock);
        public int TotalItemsCount => Products.Count;

        public ICommand RefreshCommand { get; }

        public InventoryViewModel(ApiClient api, SignalRClientService signalR)
        {
            _api = api;
            _signalR = signalR;

            RefreshCommand = new RelayCommand(async () => await LoadProductsAsync());

            // Real-time SignalR live update
            _signalR.InventoryUpdatedReceived += (productId, newStock, isLow) =>
            {
                var existing = Products.FirstOrDefault(p => p.ProductId == productId);
                if (existing != null)
                {
                    int index = Products.IndexOf(existing);
                    Products[index] = existing with { CurrentStock = newStock, IsLowStock = isLow };
                    OnPropertyChanged(nameof(LowStockCount));
                }
            };
        }

        public async Task LoadProductsAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _api.GetProductsAsync(SearchQuery);
                Products.Clear();
                foreach (var item in list)
                {
                    Products.Add(item);
                }
                OnPropertyChanged(nameof(LowStockCount));
                OnPropertyChanged(nameof(TotalItemsCount));
            }
            catch (Exception)
            {
                // Silently handle or log
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly ApiClient _api;
        private readonly SignalRClientService _signalR;
        private ViewModelBase _currentView;
        private AuthResponseDto _currentUser;
        private string _connectionStatus = "Connecting...";
        private LowStockAlertDto? _activeAlert;
        private bool _isAlertVisible;

        public BillingViewModel BillingVM { get; }
        public InventoryViewModel InventoryVM { get; }

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public AuthResponseDto CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public LowStockAlertDto? ActiveAlert
        {
            get => _activeAlert;
            set
            {
                if (SetProperty(ref _activeAlert, value))
                {
                    IsAlertVisible = value != null;
                }
            }
        }

        public bool IsAlertVisible
        {
            get => _isAlertVisible;
            set => SetProperty(ref _isAlertVisible, value);
        }

        public ICommand NavigateBillingCommand { get; }
        public ICommand NavigateInventoryCommand { get; }
        public ICommand DismissAlertCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action? OnLogoutRequested;
        public event Action<InvoiceResponseDto>? ShowReceiptDialogRequested;

        public MainViewModel(ApiClient api, SignalRClientService signalR, AuthResponseDto user)
        {
            _api = api;
            _signalR = signalR;
            _currentUser = user;

            BillingVM = new BillingViewModel(_api, _signalR);
            InventoryVM = new InventoryViewModel(_api, _signalR);
            _currentView = BillingVM;

            BillingVM.InvoiceCompleted += invoice =>
            {
                ShowReceiptDialogRequested?.Invoke(invoice);
            };

            NavigateBillingCommand = new RelayCommand(async () =>
            {
                CurrentView = BillingVM;
                await BillingVM.LoadCatalogAsync();
            });

            NavigateInventoryCommand = new RelayCommand(async () =>
            {
                CurrentView = InventoryVM;
                await InventoryVM.LoadProductsAsync();
            });

            DismissAlertCommand = new RelayCommand(() =>
            {
                IsAlertVisible = false;
                ActiveAlert = null;
            });

            LogoutCommand = new RelayCommand(async () =>
            {
                await _signalR.StopAsync();
                OnLogoutRequested?.Invoke();
            });

            _signalR.ConnectionStatusChanged += status =>
            {
                ConnectionStatus = status;
            };

            _signalR.LowStockAlertReceived += alert =>
            {
                ActiveAlert = alert;
                // Play notification sound
                try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
            };
        }

        public async Task InitializeAsync()
        {
            await _signalR.StartAsync(_currentUser.Token);
            await BillingVM.LoadCatalogAsync();
        }
    }
}
