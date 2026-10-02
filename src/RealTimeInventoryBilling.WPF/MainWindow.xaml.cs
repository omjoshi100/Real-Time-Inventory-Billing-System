using RealTimeInventoryBilling.WPF.ViewModels;
using RealTimeInventoryBilling.WPF.Views;
using System.Windows;

namespace RealTimeInventoryBilling.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void BindViewModel(MainViewModel vm)
        {
            DataContext = vm;

            vm.ShowReceiptDialogRequested += invoice =>
            {
                var receiptWindow = new ReceiptWindow(invoice)
                {
                    Owner = this
                };
                receiptWindow.ShowDialog();
            };

            vm.OnLogoutRequested += () =>
            {
                // Re-open login window
                var loginWindow = new LoginWindow();
                var loginVm = new LoginViewModel(new Services.ApiClient());
                loginWindow.DataContext = loginVm;

                loginVm.OnLoginSuccess += async auth =>
                {
                    var api = new Services.ApiClient();
                    api.SetAuthToken(auth.Token);
                    var signalR = new Services.SignalRClientService();

                    var mainVm = new MainViewModel(api, signalR, auth);
                    var newMain = new MainWindow();
                    newMain.BindViewModel(mainVm);
                    newMain.Show();
                    await mainVm.InitializeAsync();
                    loginWindow.Close();
                };

                loginWindow.Show();
                Close();
            };
        }
    }
}
