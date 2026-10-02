using Microsoft.AspNetCore.SignalR.Client;
using RealTimeInventoryBilling.Shared.Constants;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.WinForms.Services
{
    public class WinFormsApiClient
    {
        private readonly HttpClient _http;
        private string? _token;

        public string BaseUrl { get; }

        public WinFormsApiClient(string baseUrl = "http://localhost:5000")
        {
            BaseUrl = baseUrl.TrimEnd('/');
            _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        public void SetAuthToken(string? token)
        {
            _token = token;
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<AuthResponseDto?> LoginAsync(string username, string password)
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequestDto(username, password));
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {error}");
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (auth != null)
            {
                SetAuthToken(auth.Token);
            }
            return auth;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            return await _http.GetFromJsonAsync<DashboardSummaryDto>("/api/reports/dashboard-summary") 
                ?? new DashboardSummaryDto(0, 0, 0, 0, 0, 0);
        }

        public async Task<List<ProductDto>> GetProductsAsync(string? search = null, int? categoryId = null)
        {
            var url = "/api/products";
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (categoryId.HasValue) query.Add($"categoryId={categoryId.Value}");
            if (query.Count > 0) url += "?" + string.Join("&", query);

            return await _http.GetFromJsonAsync<List<ProductDto>>(url) ?? new List<ProductDto>();
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            return await _http.GetFromJsonAsync<List<CategoryDto>>("/api/inventory/categories") ?? new List<CategoryDto>();
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            var res = await _http.PostAsJsonAsync("/api/products", dto);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create product: {err}");
            }
            return (await res.Content.ReadFromJsonAsync<ProductDto>())!;
        }

        public async Task<bool> AdjustStockAsync(StockAdjustmentDto dto)
        {
            var res = await _http.PostAsJsonAsync("/api/inventory/adjust", dto);
            return res.IsSuccessStatusCode;
        }

        public async Task<List<InvoiceResponseDto>> GetRecentInvoicesAsync(int limit = 50)
        {
            return await _http.GetFromJsonAsync<List<InvoiceResponseDto>>($"/api/invoices/recent?limit={limit}") ?? new List<InvoiceResponseDto>();
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            return await _http.GetFromJsonAsync<List<UserDto>>("/api/reports/users") ?? new List<UserDto>();
        }
    }

    public class WinFormsSignalRService : IAsyncDisposable
    {
        private HubConnection? _hub;
        private readonly string _hubUrl;

        public event Action<LowStockAlertDto>? LowStockAlertReceived;
        public event Action<int, int, bool>? InventoryUpdatedReceived;
        public event Action<InvoiceResponseDto>? InvoiceCreatedReceived;
        public event Action<string>? ConnectionStatusChanged;

        public WinFormsSignalRService(string baseUrl = "http://localhost:5000")
        {
            _hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/inventory";
        }

        public async Task StartAsync(string? token = null)
        {
            if (_hub != null) await _hub.DisposeAsync();

            _hub = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            _hub.On<LowStockAlertDto>(SignalREvents.ReceiveLowStockAlert, alert => LowStockAlertReceived?.Invoke(alert));
            _hub.On<int, int, bool>(SignalREvents.ReceiveInventoryUpdate, (id, stock, low) => InventoryUpdatedReceived?.Invoke(id, stock, low));
            _hub.On<InvoiceResponseDto>(SignalREvents.ReceiveInvoiceCreated, invoice => InvoiceCreatedReceived?.Invoke(invoice));

            _hub.Reconnecting += _ => { ConnectionStatusChanged?.Invoke("Reconnecting..."); return Task.CompletedTask; };
            _hub.Reconnected += _ => { ConnectionStatusChanged?.Invoke("Connected"); return Task.CompletedTask; };
            _hub.Closed += _ => { ConnectionStatusChanged?.Invoke("Disconnected"); return Task.CompletedTask; };

            try
            {
                await _hub.StartAsync();
                ConnectionStatusChanged?.Invoke("Connected");
            }
            catch
            {
                ConnectionStatusChanged?.Invoke("Offline");
            }
        }

        public async Task StopAsync()
        {
            if (_hub != null)
            {
                await _hub.StopAsync();
                ConnectionStatusChanged?.Invoke("Disconnected");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hub != null) await _hub.DisposeAsync();
        }
    }
}
