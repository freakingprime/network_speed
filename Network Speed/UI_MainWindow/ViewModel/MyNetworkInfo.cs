using Network_Speed.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Network_Speed.UI_MainWindow.ViewModel
{
    public enum NetworkStatus
    {
        Gigabit, Bad
    }

    public class MyNetworkInfo : ViewModelBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

        public MyNetworkInfo()
        {
            InterfaceName = string.Empty;
            SpeedStr = string.Empty;
            Speed = 0;
            ConnectDuration = string.Empty;
        }

        public MyNetworkInfo(NetworkInterface a) : this()
        {
            this.Adapter = a;

            InterfaceName = Adapter.Description.Trim();
            Speed = Adapter.Speed;

            long speed = Adapter.Speed;
            string[] speedStep = { "bps", "Kbps", "Mbps", "Gbps" };
            int indexStep = 0;
            while (speed >= 1000 && indexStep < speedStep.Length - 1)
            {
                indexStep++;
                speed = speed / 1000;
            }
            SpeedStr = speed + " " + speedStep[indexStep];

            NetworkType = Adapter.NetworkInterfaceType.ToString();
        }

        #region Bind properties

        private string _networkType;

        public string NetworkType
        {
            get { return _networkType; }
            set { SetValue(ref _networkType, value); }
        }

        private string _interfaceName;

        public string InterfaceName
        {
            get { return _interfaceName; }
            set { SetValue(ref _interfaceName, value); }
        }

        private string _speedStr;

        public string SpeedStr
        {
            get { return _speedStr; }
            set { SetValue(ref _speedStr, value); }
        }

        private string _connectDuration;

        public string ConnectDuration
        {
            get { return _connectDuration; }
            set { SetValue(ref _connectDuration, value); }
        }

        private long _speed;

        public long Speed
        {
            get { return _speed; }
            set { SetValue(ref _speed, value); }
        }

        #endregion

        #region Normal properties

        public NetworkInterface Adapter = null;

        public NetworkStatus StatusSpeed
        {
            get
            {
                if (Speed >= 1000000000)
                {
                    return NetworkStatus.Gigabit;
                }
                return NetworkStatus.Bad;
            }
        }

        #endregion

        public override string ToString()
        {
            return InterfaceName + " " + SpeedStr;
        }

        public string GetFullText()
        {
            StringBuilder sb = new StringBuilder();
            AppendLine(sb, "Description", InterfaceName);
            AppendLine(sb, "Speed", SpeedStr);
            AppendLine(sb, "Type", NetworkType);
            return sb.ToString().Trim();
        }

        private void AppendLine(StringBuilder sb, string title, string value)
        {
            sb.AppendFormat("{0,-15}{1}", title + ":", value).AppendLine();
        }
    }
}
