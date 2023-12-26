using System.Windows;

namespace WpfStatus.Notification
{
    public partial class VerificationCodeDialog : Window
    {
        public VerificationCodeDialog()
        {
            InitializeComponent();
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
        }
    }
}
