using RealTimeInventoryBilling.WPF.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace RealTimeInventoryBilling.WPF.Views
{
    public partial class BillingView : UserControl
    {
        public BillingView()
        {
            InitializeComponent();
        }

        private void BarcodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is BillingViewModel vm)
            {
                if (vm.ScanBarcodeCommand.CanExecute(null))
                {
                    vm.ScanBarcodeCommand.Execute(null);
                }
            }
        }
    }
}
