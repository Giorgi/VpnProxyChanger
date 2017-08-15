using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace VpnProxyChanger
{
    class VpnDetector
    {
        public EventHandler VpnConnected;
        public EventHandler VpnDisconnected;

        private string startupIpAddress = GetPhysicalIPAdress();

        public VpnDetector()
        {
            var networkAddressChanged = Observable.FromEventPattern<NetworkAddressChangedEventHandler, EventArgs>(handler => NetworkChange.NetworkAddressChanged += handler,
                handler => NetworkChange.NetworkAddressChanged -= handler);

            networkAddressChanged.Select(pattern => GetPhysicalIPAdress())
                .DistinctUntilChanged().Subscribe(localIpAddress =>
                {
                    if (localIpAddress == "10.202.208.188")
                    {
                        VpnConnected?.Invoke(this, EventArgs.Empty);
                    }

                    if (localIpAddress == startupIpAddress)
                    {
                        VpnDisconnected?.Invoke(this, EventArgs.Empty);
                    }
                });
        }
        
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