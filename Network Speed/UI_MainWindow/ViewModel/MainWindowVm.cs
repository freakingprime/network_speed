using Network_Speed.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Network_Speed.UI_MainWindow.ViewModel
{

    public class MainWindowVm : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        private static LogController oldLog = LogController.Instance;

        public MainWindowVm()
        {
            ListRowVm = new ObservableCollection<MyNetworkInfo>();
        }

        #region Bind properties

        private string _txtIPBefore;

        public string TxtIPBefore
        {
            get { return _txtIPBefore; }
            set { SetValue(ref _txtIPBefore, value); }
        }

        private string _txtIPAfter;

        public string TxtIPAfter
        {
            get { return _txtIPAfter; }
            set { SetValue(ref _txtIPAfter, value); }
        }

        private System.Windows.Media.Brush _colorIPAfter;

        public System.Windows.Media.Brush ColorIPAfter
        {
            get { return _colorIPAfter; }
            set { SetValue(ref _colorIPAfter, value); }
        }

        public ObservableCollection<MyNetworkInfo> ListRowVm { get; set; }

        private string _txtFullInformation;

        public string TxtFullInformation
        {
            get { return _txtFullInformation; }
            set { SetValue(ref _txtFullInformation, value); }
        }

        private NetworkStatus _statusEnum;

        public NetworkStatus StatusEnum
        {
            get { return _statusEnum; }
            set { SetValue(ref _statusEnum, value); }
        }

        #endregion

        public override void Loaded()
        {
            log.Debug("Window is loaded");
            DisplayNetworkList(GetListInterface());
        }

        public List<MyNetworkInfo> GetListInterface()
        {
            log.Info("Begin get list interfaces");
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            List<MyNetworkInfo> tempList = new List<MyNetworkInfo>();
            foreach (NetworkInterface adapter in adapters)
            {
                MyNetworkInfo infoVm = new MyNetworkInfo(adapter);

                IPInterfaceProperties properties = adapter.GetIPProperties();
                IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();

                log.Info("Found: " + infoVm.ToString());

                tempList.Add(infoVm);
            }
            return tempList;
        }

        public void DisplayNetworkList(List<MyNetworkInfo> tempList)
        {
            log.Info("Update network list in UI");
            if (tempList.Count > 0)
            {
                log.Info("Found " + tempList.Count + " network interfaces. Update view.");
                ListRowVm.Clear();
                foreach (var item in tempList)
                {
                    ListRowVm.Add(item);
                }
            }
            else
            {
                log.Error("Found no network interface");
            }
        }

        public bool ShowFullInformation(string name)
        {
            bool result = false;
            TxtFullInformation = "No information of " + name;
            foreach (MyNetworkInfo item in ListRowVm)
            {
                if (item.InterfaceName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    log.Info("Found infor for: " + name);
                    TxtFullInformation = item.GetFullText();
                    StatusEnum = item.StatusSpeed;
                    result = true;
                    break;
                }
            }
            TxtFullInformation += Environment.NewLine;
            return result;
        }
        public NetworkStatus RecheckNetworkStatus()
        {
            List<MyNetworkInfo> tempList = GetListInterface();
            foreach (var item in tempList)
            {
                if (item.InterfaceName.Equals(Properties.Settings.Default.MonitoredInterface))
                {
                    return item.StatusSpeed;
                }
            }
            return NetworkStatus.Bad;
        }

    }
}
