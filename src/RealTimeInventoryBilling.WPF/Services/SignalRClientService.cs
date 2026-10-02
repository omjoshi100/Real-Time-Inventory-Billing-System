using Microsoft.AspNetCore.SignalR.Client;
using RealTimeInventoryBilling.Shared.Constants;
using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace RealTimeInventoryBilling.WPF.Services
{
    public class SignalRClientService : IAsyncDisposable
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;

        public event Action<LowStockAlertDto>? LowStockAlertReceived;
        public event Action<int, int, bool>? InventoryUpdatedReceived;
        public event Action<InvoiceResponseDto>? InvoiceCreatedReceived;
        public event Action<string>? ConnectionStatusChanged;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public SignalRClientService(string baseUrl = "http://localhost:5000")
        {
            _hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/inventory";
        }

        public async Task StartAsync(string? token = null)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<LowStockAlertDto>(SignalREvents.ReceiveLowStockAlert, alert =>
            {
                Application.Current?.Dispatcher?.Invoke(() => LowStockAlertReceived?.Invoke(alert));
            });

            _hubConnection.On<int, int, bool>(SignalREvents.ReceiveInventoryUpdate, (productId, newStock, isLowStock) =>
            {
                Application.Current?.Dispatcher?.Invoke(() => InventoryUpdatedReceived?.Invoke(productId, newStock, isLowStock));
            });

            _hubConnection.On<InvoiceResponseDto>(SignalREvents.ReceiveInvoiceCreated, invoice =>
            {
                Application.Current?.Dispatcher?.Invoke(() => InvoiceCreatedReceived?.Invoke(invoice));
            });

            _hubConnection.Reconnecting += ex =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ConnectionStatusChanged?.Invoke("Reconnecting..."));
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ConnectionStatusChanged?.Invoke("Connected"));
                return Task.CompletedTask;
            };

            _hubConnection.Closed += ex =>
            {
                Application.Current?.Dispatcher?.Invoke(() => ConnectionStatusChanged?.Invoke("Disconnected"));
                return Task.CompletedTask;
            };

            try
            {
                await _hubConnection.StartAsync();
                ConnectionStatusChanged?.Invoke("Connected");
            }
            catch (Exception)
            {
                ConnectionStatusChanged?.Invoke("Offline");
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                ConnectionStatusChanged?.Invoke("Disconnected");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
