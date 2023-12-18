using System.Diagnostics;
using System.IO;
using System.Windows;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!File.Exists("grpcurl.exe"))
            {
                var request = MessageBox.Show("The grpcurl.exe tool is used for work. Download it from the github and put it in a folder.",
                    "Need grpcurl",
                    MessageBoxButton.OKCancel);

                if (request == MessageBoxResult.OK)
                {
                    Process.Start("explorer", "https://github.com/fullstorydev/grpcurl/releases/latest");
                }

                this.Shutdown();
            }
        }
    }
}
