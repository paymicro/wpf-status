using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfStatus
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!File.Exists("grpcurl.exe"))
            {
                var request = MessageBox.Show("The grpcurl.exe tool is used for work. Download it from the github and put it in a folder.",
                    "Need grpcurl",
                    MessageBoxButton.OKCancel);

                if (request == MessageBoxResult.OK)
                {
                    await Helper.DownloadGRPCurl();
                }
            }
        }
    }
}
