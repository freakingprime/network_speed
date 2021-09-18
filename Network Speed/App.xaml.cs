using Network_Speed.UI_MainWindow.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Network_Speed
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {

        }
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (var item in e.Args)
            {
                if (item.Equals("/f") || item.Equals("/F"))
                {
                    bool ret = await MainWindowVm.ResetModemViaCommandLine();
                    if (ret)
                    {
                        Console.WriteLine("Reset successfully");
                    }
                    else
                    {
                        Console.WriteLine("Failed to reset");
                        _ = MessageBox.Show("Failed to reset modem.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Current.Shutdown();
                }
            }
        }
    }
}
