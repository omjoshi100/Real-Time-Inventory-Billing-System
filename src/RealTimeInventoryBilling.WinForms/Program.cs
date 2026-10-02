using RealTimeInventoryBilling.WinForms.Forms;
using RealTimeInventoryBilling.WinForms.Services;
using System;
using System.Windows.Forms;

namespace RealTimeInventoryBilling.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var api = new WinFormsApiClient();
            var loginForm = new LoginForm(api);

            if (loginForm.ShowDialog() == DialogResult.OK && loginForm.AuthResult != null)
            {
                var auth = loginForm.AuthResult;
                var signalR = new WinFormsSignalRService();
                Application.Run(new MainAdminForm(api, signalR, auth));
            }
        }
    }
}
