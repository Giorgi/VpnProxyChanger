using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private static string startupIpAddress = GetPhysicalIPAdress();
        private static ManualResetEvent manualResetEvent;

        static void Main(string[] args)
        {
            var networkAddressChanged = Observable.FromEventPattern<NetworkAddressChangedEventHandler, EventArgs>(handler => NetworkChange.NetworkAddressChanged += handler,
                                                                                                    handler => NetworkChange.NetworkAddressChanged -= handler);

            networkAddressChanged.Select(pattern => GetPhysicalIPAdress())
                .DistinctUntilChanged().Subscribe(localIpAddress =>
                {
                    Console.WriteLine(localIpAddress);

                    if (localIpAddress == "10.202.208.188")
                    {
                        Console.WriteLine("Enabling proxy");
                        EnableProxy();

                        if (!Process.GetProcessesByName("mstsc").Any())
                        {
                            Process.Start(@"C:\Users\Giorgi\Documents\BOG Desktop.rdp");
                        }
                    }

                    if (localIpAddress == startupIpAddress)
                    {
                        Console.WriteLine("Disabling proxy");
                        DisableProxy();

                        var mstscProcess = Process.GetProcessesByName("mstsc").FirstOrDefault();
                        if (mstscProcess != null)
                        {
                            mstscProcess.CloseMainWindow();
                            Thread.Sleep(1000);
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

        public static string GetPhysicalIPAdress()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var address = networkInterface.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (address != null && !address.Address.ToString().Equals("0.0.0.0"))
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (var ip in networkInterface.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return String.Empty;
        }
    }
}
