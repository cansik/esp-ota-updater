using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Xamarin.Forms;

[assembly: Dependency(typeof(OTAUpdater.iOSUnified.iOS.DependencyServices.IPAddressManager))]
namespace OTAUpdater.iOSUnified.iOS.DependencyServices
{
    public class IPAddressManager : IIPAddressManager
    {
        public string GetIPAddress()
        {
            var ipAddress = "";

            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            //ipAddress = addrInfo.Address.ToString();
                            return addrInfo.Address.ToString();
                        }
                    }
                }
            }

            return ipAddress;
        }
    }
}
