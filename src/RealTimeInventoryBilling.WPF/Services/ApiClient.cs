using RealTimeInventoryBilling.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RealTimeInventoryBilling.WPF.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private string? _token;

        public string BaseUrl { get; }

        public ApiClient(string baseUrl = "http://localhost:5000")
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

        public async Task<List<ProductDto>> GetProductsAsync(string? search = null)
        {
            var url = string.IsNullOrWhiteSpace(search) ? "/api/products" : $"/api/products?search={Uri.EscapeDataString(search)}";
            return await _http.GetFromJsonAsync<List<ProductDto>>(url) ?? new List<ProductDto>();
        }

        public async Task<ProductDto?> ScanProductAsync(string code)
        {
            try
            {
                return await _http.GetFromJsonAsync<ProductDto>($"/api/products/scan/{Uri.EscapeDataString(code)}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<InvoiceResponseDto> CreateInvoiceAsync(InvoiceCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync("/api/invoices", dto);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Checkout failed: {err}");
            }
            return (await response.Content.ReadFromJsonAsync<InvoiceResponseDto>())!;
        }

        public async Task<List<ProductDto>> GetLowStockAsync()
        {
            return await _http.GetFromJsonAsync<List<ProductDto>>("/api/inventory/low-stock") ?? new List<ProductDto>();
        }
    }
}
