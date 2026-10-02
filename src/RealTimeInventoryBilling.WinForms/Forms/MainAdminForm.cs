using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WinForms.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealTimeInventoryBilling.WinForms.Forms
{
    public class MainAdminForm : Form
    {
        private readonly WinFormsApiClient _api;
        private readonly WinFormsSignalRService _signalR;
        private readonly AuthResponseDto _user;

        // UI Controls
        private Panel pnlHeader = null!;
        private Label lblSignalR = null!;
        private Panel pnlAlert = null!;
        private Label lblAlertText = null!;
        private TabControl tabs = null!;

        // KPI Labels
        private Label lblRevVal = null!;
        private Label lblInvVal = null!;
        private Label lblLowVal = null!;
        private Label lblProdVal = null!;

        // Grids
        private DataGridView gridLiveSales = null!;
        private DataGridView gridProducts = null!;
        private DataGridView gridInvoices = null!;
        private DataGridView gridUsers = null!;

        private decimal _todayRevenue = 0m;
        private int _todayInvoices = 0;
        private List<ProductDto> _currentProducts = new();
        private List<CategoryDto> _categories = new();

        public MainAdminForm(WinFormsApiClient api, WinFormsSignalRService signalR, AuthResponseDto user)
        {
            _api = api;
            _signalR = signalR;
            _user = user;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = $"Admin Operations Center - {_user.FullName} ({_user.RoleName})";
            Width = 1200;
            Height = 780;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(15, 23, 42); // #0F172A
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            // 1. Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblLogo = new Label
            {
                Text = "🏢 ENTERPRISE ADMIN CONSOLE",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                AutoSize = true,
                Location = new Point(16, 16)
            };

            lblSignalR = new Label
            {
                Text = "● SignalR: Connecting...",
                ForeColor = Color.FromArgb(250, 204, 21),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(850, 20)
            };

            var btnLogout = new Button
            {
                Text = "Sign Out",
                Location = new Point(1080, 14),
                Width = 90,
                Height = 32,
                BackColor = Color.FromArgb(127, 29, 29),
                ForeColor = Color.FromArgb(254, 202, 202),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => Close();

            pnlHeader.Controls.Add(lblLogo);
            pnlHeader.Controls.Add(lblSignalR);
            pnlHeader.Controls.Add(btnLogout);

            // 2. Alert Banner
            pnlAlert = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(239, 68, 68),
                Visible = false,
                Padding = new Padding(16, 10, 16, 10)
            };

            lblAlertText = new Label
            {
                Text = "🚨 LOW STOCK ALERT",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Left
            };

            var btnDismiss = new Button
            {
                Text = "Dismiss ✕",
                Dock = DockStyle.Right,
                Width = 90,
                BackColor = Color.FromArgb(153, 27, 27),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDismiss.FlatAppearance.BorderSize = 0;
            btnDismiss.Click += (s, e) => pnlAlert.Visible = false;

            pnlAlert.Controls.Add(lblAlertText);
            pnlAlert.Controls.Add(btnDismiss);

            // 3. Tab Control
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(16, 8),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            // TAB 1: Executive Dashboard & Live Sales
            var tabDash = new TabPage("📊 Executive Dashboard & Live Ticker") { BackColor = Color.FromArgb(15, 23, 42) };
            SetupDashboardTab(tabDash);

            // TAB 2: Inventory Catalog & Restock
            var tabCatalog = new TabPage("📦 Catalog & Restock") { BackColor = Color.FromArgb(15, 23, 42) };
            SetupCatalogTab(tabCatalog);

            // TAB 3: Invoices & Reports
            var tabReports = new TabPage("📑 Invoices & Reports") { BackColor = Color.FromArgb(15, 23, 42) };
            SetupReportsTab(tabReports);

            // TAB 4: Users
            var tabUsers = new TabPage("👥 Staff Management") { BackColor = Color.FromArgb(15, 23, 42) };
            SetupUsersTab(tabUsers);

            tabs.TabPages.Add(tabDash);
            tabs.TabPages.Add(tabCatalog);
            tabs.TabPages.Add(tabReports);
            tabs.TabPages.Add(tabUsers);

            Controls.Add(tabs);
            Controls.Add(pnlAlert);
            Controls.Add(pnlHeader);

            Load += async (s, e) => await InitializeDashboardAsync();
        }

        private void SetupDashboardTab(TabPage tab)
        {
            var pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(12)
            };
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            pnlKpis.Controls.Add(CreateKpiCard("TODAY'S REVENUE", "$0.00", Color.FromArgb(16, 185, 129), out lblRevVal), 0, 0);
            pnlKpis.Controls.Add(CreateKpiCard("INVOICES ISSUED", "0", Color.FromArgb(56, 189, 248), out lblInvVal), 1, 0);
            pnlKpis.Controls.Add(CreateKpiCard("LOW STOCK ITEMS", "0", Color.FromArgb(239, 68, 68), out lblLowVal), 2, 0);
            pnlKpis.Controls.Add(CreateKpiCard("CATALOG ITEMS", "0", Color.FromArgb(168, 85, 247), out lblProdVal), 3, 0);

            var pnlLive = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var lblTickerHeader = new Label
            {
                Text = "⚡ REAL-TIME LIVE INVOICE TICKER (Streamed via SignalR WebSockets)",
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 35
            };

            gridLiveSales = CreateStyledDataGrid();
            gridLiveSales.Columns.Add("InvoiceNo", "Invoice #");
            gridLiveSales.Columns.Add("Time", "Time (UTC)");
            gridLiveSales.Columns.Add("Cashier", "Cashier Terminal");
            gridLiveSales.Columns.Add("Customer", "Customer");
            gridLiveSales.Columns.Add("ItemsCount", "Items");
            gridLiveSales.Columns.Add("Total", "Grand Total ($)");
            gridLiveSales.Columns.Add("Payment", "Payment");
            gridLiveSales.Dock = DockStyle.Fill;

            pnlLive.Controls.Add(gridLiveSales);
            pnlLive.Controls.Add(lblTickerHeader);

            tab.Controls.Add(pnlLive);
            tab.Controls.Add(pnlKpis);
        }

        private void SetupCatalogTab(TabPage tab)
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(12)
            };

            var btnAdd = new Button
            {
                Text = "+ Add New Product",
                Width = 160,
                Height = 35,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += async (s, e) =>
            {
                var dlg = new AddProductForm(_api, _categories);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    await LoadCatalogAsync();
                }
            };

            var btnRestock = new Button
            {
                Text = "📦 Adjust Stock / Restock",
                Width = 200,
                Height = 35,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.FromArgb(2, 44, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnRestock.FlatAppearance.BorderSize = 0;
            btnRestock.Margin = new Padding(10, 0, 0, 0);
            btnRestock.Click += async (s, e) =>
            {
                ProductDto? sel = null;
                if (gridProducts.SelectedRows.Count > 0 && gridProducts.SelectedRows[0].Tag is ProductDto p)
                {
                    sel = p;
                }
                var dlg = new StockAdjustmentDialog(_api, _currentProducts, sel);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    await LoadCatalogAsync();
                }
            };

            var btnRefresh = new Button
            {
                Text = "↻ Refresh",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await LoadCatalogAsync();

            pnlTop.Controls.Add(btnRestock);
            pnlTop.Controls.Add(btnAdd);
            pnlTop.Controls.Add(btnRefresh);

            gridProducts = CreateStyledDataGrid();
            gridProducts.Columns.Add("SKU", "SKU");
            gridProducts.Columns.Add("Name", "Product Name");
            gridProducts.Columns.Add("Category", "Category");
            gridProducts.Columns.Add("Price", "Retail ($)");
            gridProducts.Columns.Add("Cost", "Cost ($)");
            gridProducts.Columns.Add("Stock", "Current Stock");
            gridProducts.Columns.Add("Threshold", "Min Threshold");
            gridProducts.Columns.Add("Status", "Inventory Status");
            gridProducts.Dock = DockStyle.Fill;

            var pnlContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            pnlContainer.Controls.Add(gridProducts);

            tab.Controls.Add(pnlContainer);
            tab.Controls.Add(pnlTop);
        }

        private void SetupReportsTab(TabPage tab)
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(12)
            };

            var btnExport = new Button
            {
                Text = "📥 Export Invoices to CSV",
                Width = 200,
                Height = 35,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ExportInvoicesToCsv();

            pnlTop.Controls.Add(btnExport);

            gridInvoices = CreateStyledDataGrid();
            gridInvoices.Columns.Add("InvoiceId", "ID");
            gridInvoices.Columns.Add("InvoiceNo", "Invoice #");
            gridInvoices.Columns.Add("Date", "Date");
            gridInvoices.Columns.Add("Cashier", "Cashier");
            gridInvoices.Columns.Add("Customer", "Customer");
            gridInvoices.Columns.Add("SubTotal", "SubTotal ($)");
            gridInvoices.Columns.Add("Tax", "Tax ($)");
            gridInvoices.Columns.Add("Total", "Grand Total ($)");
            gridInvoices.Columns.Add("Payment", "Payment");
            gridInvoices.Dock = DockStyle.Fill;

            var pnlContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            pnlContainer.Controls.Add(gridInvoices);

            tab.Controls.Add(pnlContainer);
            tab.Controls.Add(pnlTop);
        }

        private void SetupUsersTab(TabPage tab)
        {
            gridUsers = CreateStyledDataGrid();
            gridUsers.Columns.Add("UserId", "User ID");
            gridUsers.Columns.Add("Username", "Username");
            gridUsers.Columns.Add("FullName", "Full Name");
            gridUsers.Columns.Add("Email", "Email");
            gridUsers.Columns.Add("Role", "Security Role");
            gridUsers.Columns.Add("Status", "Account Status");
            gridUsers.Dock = DockStyle.Fill;

            var pnlContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            pnlContainer.Controls.Add(gridUsers);
            tab.Controls.Add(pnlContainer);
        }

        private Control CreateKpiCard(string title, string value, Color accent, out Label valLabel)
        {
            var pnl = new Panel
            {
                BackColor = Color.FromArgb(30, 41, 59),
                Margin = new Padding(6),
                Dock = DockStyle.Fill,
                Padding = new Padding(14)
            };

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 22
            };

            valLabel = new Label
            {
                Text = value,
                ForeColor = accent,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(valLabel);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private DataGridView CreateStyledDataGrid()
        {
            var grid = new DataGridView
            {
                BackgroundColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(51, 65, 85),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(37, 99, 235);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(148, 163, 184);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private async Task InitializeDashboardAsync()
        {
            // Connect to SignalR
            _signalR.ConnectionStatusChanged += status =>
            {
                BeginInvoke(new Action(() =>
                {
                    lblSignalR.Text = $"● SignalR: {status}";
                    lblSignalR.ForeColor = status == "Connected" ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
                }));
            };

            _signalR.InvoiceCreatedReceived += invoice =>
            {
                BeginInvoke(new Action(() =>
                {
                    // Prepend new invoice to Live Ticker
                    gridLiveSales.Rows.Insert(0, invoice.InvoiceNumber, invoice.CreatedAt.ToString("HH:mm:ss"), invoice.CashierName, invoice.CustomerName, invoice.Items.Count, invoice.GrandTotal.ToString("F2"), invoice.PaymentMethod);
                    gridLiveSales.Rows[0].DefaultCellStyle.BackColor = Color.FromArgb(22, 101, 52); // Flash Green

                    _todayRevenue += invoice.GrandTotal;
                    _todayInvoices++;
                    lblRevVal.Text = $"${_todayRevenue:N2}";
                    lblInvVal.Text = _todayInvoices.ToString();
                }));
            };

            _signalR.LowStockAlertReceived += alert =>
            {
                BeginInvoke(new Action(() =>
                {
                    lblAlertText.Text = $"🚨 LOW STOCK ALERT: '{alert.Name}' (SKU: {alert.SKU}) has only {alert.CurrentStock} units left in stock!";
                    pnlAlert.Visible = true;
                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
                    _ = LoadCatalogAsync();
                }));
            };

            _signalR.InventoryUpdatedReceived += (prodId, newStock, isLow) =>
            {
                BeginInvoke(new Action(() =>
                {
                    foreach (DataGridViewRow row in gridProducts.Rows)
                    {
                        if (row.Tag is ProductDto p && p.ProductId == prodId)
                        {
                            row.Cells["Stock"].Value = newStock;
                            row.Cells["Status"].Value = isLow ? "⚠️ LOW STOCK" : "In Stock";
                            row.DefaultCellStyle.ForeColor = isLow ? Color.FromArgb(248, 113, 113) : Color.White;
                            break;
                        }
                    }
                }));
            };

            await _signalR.StartAsync(_user.Token);

            // Initial Data Fetch
            await LoadDashboardSummaryAsync();
            await LoadCatalogAsync();
            await LoadRecentInvoicesAsync();
            await LoadUsersAsync();
        }

        private async Task LoadDashboardSummaryAsync()
        {
            try
            {
                var summary = await _api.GetDashboardSummaryAsync();
                _todayRevenue = summary.TodayRevenue;
                _todayInvoices = summary.TodayInvoicesCount;
                lblRevVal.Text = $"${summary.TodayRevenue:N2}";
                lblInvVal.Text = summary.TodayInvoicesCount.ToString();
                lblLowVal.Text = summary.LowStockCount.ToString();
                lblProdVal.Text = summary.TotalProductsCount.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard load failed: {ex.Message}");
            }
        }

        private async Task LoadCatalogAsync()
        {
            try
            {
                _categories = await _api.GetCategoriesAsync();
                _currentProducts = await _api.GetProductsAsync();

                gridProducts.Rows.Clear();
                foreach (var p in _currentProducts)
                {
                    int idx = gridProducts.Rows.Add(
                        p.SKU,
                        p.Name,
                        p.CategoryName,
                        $"${p.UnitPrice:F2}",
                        $"${p.CostPrice:F2}",
                        p.CurrentStock,
                        p.LowStockThreshold,
                        p.IsLowStock ? "⚠️ LOW STOCK" : "In Stock"
                    );
                    gridProducts.Rows[idx].Tag = p;
                    if (p.IsLowStock)
                    {
                        gridProducts.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Catalog load error: {ex.Message}");
            }
        }

        private async Task LoadRecentInvoicesAsync()
        {
            try
            {
                var list = await _api.GetRecentInvoicesAsync(50);
                gridInvoices.Rows.Clear();
                gridLiveSales.Rows.Clear();

                foreach (var inv in list)
                {
                    gridInvoices.Rows.Add(
                        inv.InvoiceId,
                        inv.InvoiceNumber,
                        inv.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        inv.CashierName,
                        inv.CustomerName,
                        $"${inv.SubTotal:F2}",
                        $"${inv.TaxAmount:F2}",
                        $"${inv.GrandTotal:F2}",
                        inv.PaymentMethod
                    );

                    gridLiveSales.Rows.Add(
                        inv.InvoiceNumber,
                        inv.CreatedAt.ToString("HH:mm:ss"),
                        inv.CashierName,
                        inv.CustomerName,
                        inv.Items.Count,
                        inv.GrandTotal.ToString("F2"),
                        inv.PaymentMethod
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invoices load error: {ex.Message}");
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _api.GetUsersAsync();
                gridUsers.Rows.Clear();
                foreach (var u in users)
                {
                    gridUsers.Rows.Add(
                        u.UserId,
                        u.Username,
                        u.FullName,
                        u.Email ?? "N/A",
                        u.RoleName,
                        u.IsActive ? "Active" : "Disabled"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Users load error: {ex.Message}");
            }
        }

        private void ExportInvoicesToCsv()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV Spreadsheet (*.csv)|*.csv",
                FileName = $"Invoices_Report_{DateTime.UtcNow:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var sw = new StreamWriter(sfd.FileName);
                    sw.WriteLine("InvoiceId,InvoiceNumber,Date,Cashier,Customer,SubTotal,TaxAmount,GrandTotal,PaymentMethod");
                    foreach (DataGridViewRow row in gridInvoices.Rows)
                    {
                        var cells = row.Cells.Cast<DataGridViewCell>().Select(c => $"\"{c.Value}\"");
                        sw.WriteLine(string.Join(",", cells));
                    }
                    MessageBox.Show("Invoices exported to CSV successfully!", "Report Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
