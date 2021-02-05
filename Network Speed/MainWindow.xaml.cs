using Network_Speed.UI_MainWindow.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Network_Speed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MainWindow()
        {
            InitializeComponent();
            this.Title = Properties.Resources.TITLE + " " + Properties.Resources.VERSION + "." + Properties.Resources.BuildTime;
#if DEBUG
            this.Title = this.Title + " DEBUG";
#endif
            string startMessage = "Program started at: " + DateTime.Now.ToString();
            log.Debug(startMessage);
            Console.WriteLine(startMessage);
        }

        #region Normal properties

        private MainWindowVm vm = null;

        #endregion

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.vm = e.NewValue as MainWindowVm;
            if (vm != null)
            {
                log.Debug("New view model from Window_DataContextChanged");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.vm = this.DataContext as MainWindowVm;
            if (vm != null)
            {
                log.Debug("New view model from Window_Loaded");
                vm.Loaded();
            }
            TxtMonitorInterface.Text = Properties.Settings.Default.MonitoredInterface;
        }

        private void TxtMonitorInterface_TextChanged(object sender, TextChangedEventArgs e)
        {
            string name = ((TextBox)sender).Text.Trim();
            if (vm.ShowFullInformation(name))
            {
                log.Info("Save this valid name: " + name);
                Properties.Settings.Default.MonitoredInterface = name;
                Properties.Settings.Default.Save();
            }
        }

        private void MenuMonitorThis_Click(object sender, RoutedEventArgs e)
        {
            log.Info("Monitor this item");
        }

        private void DataGridAllInterface_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                log.Info("Ctrl C is pressed");
                if (DataGridAllInterface.SelectedItem is MyNetworkInfo info && info.InterfaceName.Length > 0)
                {
                    Clipboard.SetText(info.InterfaceName);
                    log.Info("Copied name: " + info.InterfaceName);
                    TxtMonitorInterface.Text = info.InterfaceName;
                    e.Handled = true;
                }
            }
        }

        private void BtnReloadInformation_Click(object sender, RoutedEventArgs e)
        {
            TxtMonitorInterface.Text = Properties.Settings.Default.MonitoredInterface;
            vm.ShowFullInformation(TxtMonitorInterface.Text);
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                log.Debug("Double click grid row");
                if (((DataGridRow)sender).DataContext is MyNetworkInfo info)
                {
                    vm.ShowFullInformation(info.InterfaceName);
                }
            }
        }
    }
}