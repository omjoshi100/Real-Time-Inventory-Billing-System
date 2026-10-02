using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WinForms.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RealTimeInventoryBilling.WinForms.Forms
{
    public class LoginForm : Form
    {
        private readonly WinFormsApiClient _api;
        public AuthResponseDto? AuthResult { get; private set; }

        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Label lblError = null!;
        private Button btnLogin = null!;

        public LoginForm(WinFormsApiClient api)
        {
            _api = api;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Admin Portal - Real-Time Inventory & Billing System";
            Width = 460;
            Height = 520;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(15, 23, 42); // #0F172A
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(32)
            };

            var lblIcon = new Label
            {
                Text = "🏢",
                Font = new Font("Segoe UI", 28F),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60,
                ForeColor = Color.FromArgb(56, 189, 248)
            };

            var lblTitle = new Label
            {
                Text = "ADMIN & OPERATIONS",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 35,
                ForeColor = Color.FromArgb(248, 250, 252)
            };

            var lblSubtitle = new Label
            {
                Text = "Enterprise Windows Forms Control Center",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = Color.FromArgb(148, 163, 184)
            };

            lblError = new Label
            {
                ForeColor = Color.FromArgb(248, 113, 113),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            var pnlForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220
            };

            var lblUser = new Label { Text = "Username", ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(10, 10), AutoSize = true };
            txtUsername = new TextBox
            {
                Text = "admin",
                Location = new Point(10, 32),
                Width = 360,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11F)
            };

            var lblPass = new Label { Text = "Password", ForeColor = Color.FromArgb(203, 213, 225), Location = new Point(10, 75), AutoSize = true };
            txtPassword = new TextBox
            {
                Text = "Admin@123",
                PasswordChar = '●',
                Location = new Point(10, 97),
                Width = 360,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11F)
            };

            btnLogin = new Button
            {
                Text = "SIGN IN TO DASHBOARD",
                Location = new Point(10, 145),
                Width = 360,
                Height = 42,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += async (s, e) => await PerformLoginAsync();

            var pnlQuick = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                FlowDirection = FlowDirection.LeftToRight
            };

            var btnAdmin = new Button { Text = "Admin Preset", Width = 115, Height = 32, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.FromArgb(56, 189, 248), FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            btnAdmin.Click += (s, e) => { txtUsername.Text = "admin"; txtPassword.Text = "Admin@123"; };

            var btnManager = new Button { Text = "Manager Preset", Width = 115, Height = 32, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.FromArgb(56, 189, 248), FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            btnManager.Click += (s, e) => { txtUsername.Text = "manager1"; txtPassword.Text = "Manager@123"; };

            var btnCashier = new Button { Text = "Cashier Preset", Width = 115, Height = 32, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.FromArgb(56, 189, 248), FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
            btnCashier.Click += (s, e) => { txtUsername.Text = "cashier1"; txtPassword.Text = "Cashier@123"; };

            pnlQuick.Controls.Add(btnAdmin);
            pnlQuick.Controls.Add(btnManager);
            pnlQuick.Controls.Add(btnCashier);

            pnlForm.Controls.Add(lblUser);
            pnlForm.Controls.Add(txtUsername);
            pnlForm.Controls.Add(lblPass);
            pnlForm.Controls.Add(txtPassword);
            pnlForm.Controls.Add(btnLogin);

            pnl.Controls.Add(pnlQuick);
            pnl.Controls.Add(pnlForm);
            pnl.Controls.Add(lblError);
            pnl.Controls.Add(lblSubtitle);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblIcon);

            Controls.Add(pnl);
        }

        private async System.Threading.Tasks.Task PerformLoginAsync()
        {
            lblError.Visible = false;
            btnLogin.Enabled = false;
            btnLogin.Text = "AUTHENTICATING...";

            try
            {
                var auth = await _api.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text.Trim());
                if (auth != null)
                {
                    AuthResult = auth;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message.Replace("Login failed: ", "");
                lblError.Visible = true;
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "SIGN IN TO DASHBOARD";
            }
        }
    }
}
