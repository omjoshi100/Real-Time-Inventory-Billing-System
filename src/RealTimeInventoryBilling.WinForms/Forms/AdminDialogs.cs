using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WinForms.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RealTimeInventoryBilling.WinForms.Forms
{
    public class AddProductForm : Form
    {
        private readonly WinFormsApiClient _api;
        private readonly List<CategoryDto> _categories;

        private TextBox txtSKU = null!;
        private TextBox txtBarcode = null!;
        private TextBox txtName = null!;
        private ComboBox cmbCategory = null!;
        private NumericUpDown numUnitPrice = null!;
        private NumericUpDown numCostPrice = null!;
        private NumericUpDown numInitialStock = null!;
        private NumericUpDown numThreshold = null!;
        private TextBox txtUnit = null!;
        private Button btnSave = null!;

        public AddProductForm(WinFormsApiClient api, List<CategoryDto> categories)
        {
            _api = api;
            _categories = categories;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Add New Product - Catalog Management";
            Width = 460;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var pnl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 10
            };
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            txtSKU = new TextBox { Width = 220, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtBarcode = new TextBox { Width = 220, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtName = new TextBox { Width = 220, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            cmbCategory = new ComboBox { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };

            foreach (var cat in _categories)
            {
                cmbCategory.Items.Add(new ComboBoxItem(cat.Name, cat.CategoryId));
            }
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;

            numUnitPrice = new NumericUpDown { Width = 120, DecimalPlaces = 2, Maximum = 10000, Value = 9.99m, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            numCostPrice = new NumericUpDown { Width = 120, DecimalPlaces = 2, Maximum = 10000, Value = 4.50m, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            numInitialStock = new NumericUpDown { Width = 120, Maximum = 10000, Value = 25, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            numThreshold = new NumericUpDown { Width = 120, Maximum = 1000, Value = 5, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            txtUnit = new TextBox { Text = "pcs", Width = 120, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            btnSave = new Button
            {
                Text = "SAVE PRODUCT TO DATABASE",
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.Click += async (s, e) => await SaveProductAsync();

            AddRow(pnl, "SKU Code *", txtSKU);
            AddRow(pnl, "Barcode", txtBarcode);
            AddRow(pnl, "Product Name *", txtName);
            AddRow(pnl, "Category", cmbCategory);
            AddRow(pnl, "Retail Price ($)", numUnitPrice);
            AddRow(pnl, "Cost Price ($)", numCostPrice);
            AddRow(pnl, "Initial Stock", numInitialStock);
            AddRow(pnl, "Low Stock Alert At", numThreshold);
            AddRow(pnl, "Unit Measure", txtUnit);

            pnl.Controls.Add(btnSave, 1, 9);
            Controls.Add(pnl);
        }

        private void AddRow(TableLayoutPanel pnl, string label, Control control)
        {
            var lbl = new Label { Text = label, AutoSize = true, ForeColor = Color.FromArgb(203, 213, 225), Anchor = AnchorStyles.Left };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
        }

        private async System.Threading.Tasks.Task SaveProductAsync()
        {
            if (string.IsNullOrWhiteSpace(txtSKU.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("SKU and Product Name are required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int categoryId = (cmbCategory.SelectedItem as ComboBoxItem)?.Value ?? 1;

            var dto = new CreateProductDto(
                txtSKU.Text.Trim(),
                string.IsNullOrWhiteSpace(txtBarcode.Text) ? null : txtBarcode.Text.Trim(),
                txtName.Text.Trim(),
                null,
                categoryId,
                numUnitPrice.Value,
                numCostPrice.Value,
                (int)numInitialStock.Value,
                (int)numThreshold.Value,
                txtUnit.Text.Trim()
            );

            try
            {
                await _api.CreateProductAsync(dto);
                MessageBox.Show("Product created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ComboBoxItem
        {
            public string Text { get; }
            public int Value { get; }
            public ComboBoxItem(string text, int value) { Text = text; Value = value; }
            public override string ToString() => Text;
        }
    }

    public class StockAdjustmentDialog : Form
    {
        private readonly WinFormsApiClient _api;
        private readonly List<ProductDto> _products;

        private ComboBox cmbProduct = null!;
        private NumericUpDown numQuantity = null!;
        private ComboBox cmbType = null!;
        private TextBox txtReason = null!;
        private Label lblCurrentStock = null!;
        private Button btnAdjust = null!;

        public StockAdjustmentDialog(WinFormsApiClient api, List<ProductDto> products, ProductDto? selected = null)
        {
            _api = api;
            _products = products;
            InitializeComponent(selected);
        }

        private void InitializeComponent(ProductDto? selected)
        {
            Text = "Adjust Stock & Restock - ACID Stored Procedure";
            Width = 480;
            Height = 440;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var pnl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 6
            };
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            pnl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            cmbProduct = new ComboBox { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            int selectedIdx = 0;
            for (int i = 0; i < _products.Count; i++)
            {
                var p = _products[i];
                cmbProduct.Items.Add(new ComboBoxProductItem($"{p.Name} ({p.SKU})", p.ProductId, p.CurrentStock));
                if (selected != null && p.ProductId == selected.ProductId) selectedIdx = i;
            }
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = selectedIdx;

            lblCurrentStock = new Label { Text = "Current Stock: 0", AutoSize = true, ForeColor = Color.FromArgb(56, 189, 248), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            cmbProduct.SelectedIndexChanged += (s, e) =>
            {
                if (cmbProduct.SelectedItem is ComboBoxProductItem item)
                {
                    lblCurrentStock.Text = $"Current Stock: {item.Stock} units";
                }
            };
            if (cmbProduct.SelectedItem is ComboBoxProductItem initItem)
            {
                lblCurrentStock.Text = $"Current Stock: {initItem.Stock} units";
            }

            numQuantity = new NumericUpDown
            {
                Width = 140,
                Minimum = -500,
                Maximum = 5000,
                Value = 20,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White
            };

            cmbType = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            cmbType.Items.AddRange(new object[] { "Purchase", "Adjustment", "Return" });
            cmbType.SelectedIndex = 0;

            txtReason = new TextBox
            {
                Text = "Supplier Restock Shipment Received",
                Width = 260,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnAdjust = new Button
            {
                Text = "EXECUTE STOCK ADJUSTMENT",
                Dock = DockStyle.Fill,
                Height = 42,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.FromArgb(2, 44, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdjust.Click += async (s, e) => await ExecuteAdjustmentAsync();

            AddRow(pnl, "Product Target", cmbProduct);
            AddRow(pnl, "Inventory Level", lblCurrentStock);
            AddRow(pnl, "Adjustment Qty", numQuantity);
            AddRow(pnl, "Movement Type", cmbType);
            AddRow(pnl, "Reason / Note", txtReason);

            pnl.Controls.Add(btnAdjust, 1, 5);
            Controls.Add(pnl);
        }

        private void AddRow(TableLayoutPanel pnl, string label, Control control)
        {
            var lbl = new Label { Text = label, AutoSize = true, ForeColor = Color.FromArgb(203, 213, 225), Anchor = AnchorStyles.Left };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(control);
        }

        private async System.Threading.Tasks.Task ExecuteAdjustmentAsync()
        {
            if (numQuantity.Value == 0)
            {
                MessageBox.Show("Quantity change cannot be zero.", "Invalid Adjustment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbProduct.SelectedItem is not ComboBoxProductItem item) return;

            var dto = new StockAdjustmentDto(
                item.ProductId,
                (int)numQuantity.Value,
                cmbType.SelectedItem?.ToString() ?? "Adjustment",
                txtReason.Text.Trim()
            );

            try
            {
                bool ok = await _api.AdjustStockAsync(dto);
                if (ok)
                {
                    MessageBox.Show("Stock adjusted successfully! SignalR update dispatched to all screens.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to adjust stock.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Adjustment failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ComboBoxProductItem
        {
            public string Name { get; }
            public int ProductId { get; }
            public int Stock { get; }
            public ComboBoxProductItem(string name, int productId, int stock) { Name = name; ProductId = productId; Stock = stock; }
            public override string ToString() => Name;
        }
    }
}
