using Network_Speed.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
        private static HttpClient client = new HttpClient();

        private static string GetBase64(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public void ButtonTest()
        {

        }

        public static async Task<bool> ResetModemAtAllCost()
        {
            bool result = false;
            const int MAX_TRY = 3;
            for (int i = 1; i <= MAX_TRY; ++i)
            {
                if (await ResetModemOnce())
                {
                    result = true;
                    break;
                }
                else
                {
                    Thread.Sleep(500);
                    if (i < MAX_TRY)
                    {
                        oldLog.Error("Try again (" + i + ")");
                    }
                }
            }
            return result;
        }

        public static async Task<bool> ResetModemOnce()
        {
            bool result = false;

            string sessionID = string.Empty;
            string md5 = string.Empty;
            string csrf = string.Empty;
            Uri address = new Uri("https://192.168.1.1");
            string username = "admin";
            string password = "Adminis1@";
            string auth = "uid =" + username + "; psw=" + password;
            string base64 = GetBase64(auth);

            HttpResponseMessage response = null;
            string responseString = "";

            oldLog.Debug(Environment.NewLine);
            oldLog.Debug("Begin resetting modem");

            try
            {
                using (var httpClientHandler = new HttpClientHandler() { CookieContainer = new CookieContainer() })
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                    client = new HttpClient(httpClientHandler) { BaseAddress = address };

                    //get session id, doesn't need cookies
                    response = await client.GetAsync("/cgi-bin/login.asp");
                    if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> headerValues))
                    {
                        string setcookie = headerValues.FirstOrDefault();
                        const string prefix = "SESSIONID=";
                        int k = setcookie.IndexOf(prefix);
                        if (k >= 0)
                        {
                            setcookie = setcookie.Substring(k + prefix.Length);
                        }
                        k = setcookie.IndexOf(";");
                        sessionID = k >= 0 ? setcookie.Remove(k) : setcookie;
                    }
                    oldLog.Debug("SessionID: " + (sessionID.Length > 0 ? sessionID : "None"));
                    if (sessionID.Length > 0)
                    {
                        //get md5
                        httpClientHandler.CookieContainer.Add(address, new Cookie("base64", base64));
                        response = await client.GetAsync("/cgi-bin/index.asp");
                        responseString = await response.Content.ReadAsStringAsync();
                        log.Info("MD5 length: " + responseString.Length);
                        // var auth = \"$1$n.SFV13E$mKTcMiUeShDlsv/2iUrBI0\"
                        Regex regexMD5 = new Regex(@"var auth.*?""([^""]+)", RegexOptions.IgnoreCase);
                        Match matchMD5 = regexMD5.Match(responseString);
                        if (matchMD5.Success)
                        {
                            md5 = matchMD5.Groups[1].Value.Trim();
                        }
                        oldLog.Debug("MD5: " + (md5.Length > 0 ? md5 : "None"));

                        //get csrf
                        httpClientHandler.CookieContainer.Add(address, new Cookie("SESSIONID", sessionID));
                        response = await client.GetAsync("/cgi-bin/login.asp");
                        responseString = await response.Content.ReadAsStringAsync();
                        log.Info("CSRF page length: " + responseString.Length);
                        //NAME=\"CsrfToken\" VALUE=\"baqk4w7RoMZNHdnEISAeczxhDYvqdwCg\">\r\n
                        Regex regexCsrf = new Regex(@"name=.+?csrftoken.{0,5}value.*?""([^""]*)""", RegexOptions.IgnoreCase);
                        Match matchCsrf = regexCsrf.Match(responseString);
                        if (matchCsrf.Success)
                        {
                            csrf = matchCsrf.Groups[1].Value.Trim();
                        }
                        oldLog.Debug("CSRF: " + (csrf.Length > 0 ? csrf : "None"));
                    }
                }

                if (sessionID.Length > 0 && md5.Length > 0 && csrf.Length > 0)
                {
                    //submit reset form
                    using (var httpClientHandler = new HttpClientHandler() { CookieContainer = new CookieContainer() })
                    {
                        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                        httpClientHandler.CookieContainer.Add(address, new Cookie("SESSIONID", sessionID));
                        httpClientHandler.CookieContainer.Add(address, new Cookie("md5", md5));
                        client = new HttpClient(httpClientHandler) { BaseAddress = address };
                        var values = new Dictionary<string, string> { { "testFlag", "0" }, { "restoreFlag", "1" }, { "rebootFlag", "1" }, { "CsrfToken", csrf } };
                        var content = new FormUrlEncodedContent(values);
                        response = await client.PostAsync("/cgi-bin/tools_system.asp", content);
                        responseString = await response.Content.ReadAsStringAsync();
                        log.Info("Reset page length: " + responseString.Length);
                        //File.WriteAllText(@"D:\DOWNLOADED\test.txt", responseString);
                        if (Regex.Match(responseString, @"Please wait for reboot complete", RegexOptions.IgnoreCase).Success)
                        {
                            result = true;
                        }
                    }
                }
            }
            catch (Exception e1)
            {
                oldLog.Error("Exception: " + e1.GetType().Name + ". " + (e1.Message ?? "No message"));
            }
            if (result)
            {
                oldLog.Debug("Finish reset modem. Please check real status.");
            }
            else
            {
                oldLog.Error("Failed to reset modem");
            }
            return result;
        }

        public async void ButtonResetModem()
        {
#if DEBUG
            MessageBoxResult ret = MessageBoxResult.Yes;
#else
            MessageBoxResult ret = MessageBox.Show("Do you want to reset modem?", "Reset modem", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
#endif
            if (ret == MessageBoxResult.Yes)
            {
                await ResetModemAtAllCost();
            }
        }

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
            TxtFullInformation = "No information of " + name;
            foreach (MyNetworkInfo item in ListRowVm)
            {
                if (item.InterfaceName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    log.Info("Found infor for: " + name);
                    TxtFullInformation = item.GetFullText();
                    StatusEnum = item.StatusSpeed;
                    return true;
                }
            }
            return false;
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
