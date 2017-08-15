using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace VpnProxyChanger
{
    class Program
    {
        private static ManualResetEvent manualResetEvent;

        static void Main(string[] args)
        {
            var vpnDetector = new VpnDetector();
            vpnDetector.VpnConnected += (sender, eventArgs) =>
            {
                EnableProxy();

                if (!Process.GetProcessesByName("mstsc").Any())
                {
                    Process.Start(@"C:\Users\Giorgi\Documents\BOG Desktop.rdp");
                }
            };

            vpnDetector.VpnDisconnected += (sender, eventArgs) =>
            {
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
            };

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
    }
}
