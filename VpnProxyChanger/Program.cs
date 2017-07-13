using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VpnProxyChanger
{
    class Program
    {
        private static string startupIpAddress = GetLocalIPAddress();
        private static ManualResetEvent manualResetEvent;
        private static Process mstscProcess;

        static void Main(string[] args)
        {
            var networkAddressChanged = Observable.FromEventPattern<NetworkAddressChangedEventHandler, EventArgs>(handler => NetworkChange.NetworkAddressChanged += handler,
                                                                                                    handler => NetworkChange.NetworkAddressChanged -= handler);
            
            
            networkAddressChanged.Throttle(TimeSpan.FromSeconds(3)).Subscribe(pattern =>
            {
                var localIpAddress = GetLocalIPAddress();

                Console.WriteLine(localIpAddress);

                if (localIpAddress == "10.202.208.188")
                {
                    Console.WriteLine("Enabling proxy");
                    EnableProxy();

                    if (!Process.GetProcessesByName("mstsc").Any())
                    {
                        mstscProcess = Process.Start(@"C:\Users\Giorgi\Documents\BOG Desktop.rdp");
                    }
                }

                if (localIpAddress == startupIpAddress)
                {
                    Console.WriteLine("Disabling proxy");
                    DisableProxy();

                    if (mstscProcess != null)
                    {
                        mstscProcess.CloseMainWindow();
                        Thread.Sleep(3000);
                        if (!mstscProcess.HasExited)
                        {
                            mstscProcess.Kill();
                        }
                    }
                }
            });

            manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
        }

        private static void EnableProxy()
        {
            using (var registry = Registry.CurrentUser.OpenSubKey(ProxySettingsKey, true))
            {
                registry.SetValue("ProxyEnable", 1);
                registry.SetValue("ProxyServer", "thegate.bog.ge:8080");
            }

            NotifyProxyChanged();
        }

        private static void DisableProxy()
        {
            using (var registry = Registry.CurrentUser.OpenSubKey(ProxySettingsKey, true))
            {
                registry.SetValue("ProxyEnable", 0);
            }

            NotifyProxyChanged();
        }

        private static void NotifyProxyChanged()
        {
            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
        }

        // Define other methods and classes here
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public const int InternetOptionRefresh = 37;
        public const int InternetOptionSettingsChanged = 39;
        private const string ProxySettingsKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}
