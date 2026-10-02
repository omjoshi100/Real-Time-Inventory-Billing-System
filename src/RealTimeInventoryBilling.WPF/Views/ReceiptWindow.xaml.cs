using RealTimeInventoryBilling.Shared.DTOs;
using RealTimeInventoryBilling.WPF.Services;
using System.Windows;

namespace RealTimeInventoryBilling.WPF.Views
{
    public partial class ReceiptWindow : Window
    {
        public ReceiptWindow(InvoiceResponseDto invoice)
        {
            InitializeComponent();
            TxtReceipt.Text = ReceiptPrintService.GenerateReceiptText(invoice);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtReceipt.Text);
            MessageBox.Show("Receipt text has been copied to clipboard for printing!", "Receipt Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
