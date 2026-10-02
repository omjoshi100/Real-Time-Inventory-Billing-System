using RealTimeInventoryBilling.WPF.Services;
using RealTimeInventoryBilling.WPF.ViewModels;
using RealTimeInventoryBilling.WPF.Views;
using System.Windows;

namespace RealTimeInventoryBilling.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var api = new ApiClient();
            var loginVm = new LoginViewModel(api);
            var loginWindow = new LoginWindow
            {
                DataContext = loginVm
            };

            loginVm.OnLoginSuccess += async auth =>
            {
                api.SetAuthToken(auth.Token);
                var signalR = new SignalRClientService();

                var mainVm = new MainViewModel(api, signalR, auth);
                var mainWindow = new MainWindow();
                mainWindow.BindViewModel(mainVm);

                mainWindow.Show();
                await mainVm.InitializeAsync();

                loginWindow.Close();
            };

            loginWindow.Show();
        }
    }
}
