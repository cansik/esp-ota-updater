using System;
using System.Net;
using Xamarin.Forms;

[assembly: Dependency(typeof(OTAUpdater.Droid.DependencyServices.IPAddressManager))]
namespace OTAUpdater.Droid.DependencyServices
{
    class IPAddressManager : IIPAddressManager
    {
        public string GetIPAddress()
        {
            IPAddress[] adresses = Dns.GetHostAddresses(Dns.GetHostName());

            if (adresses != null && adresses[0] != null)
            {
                return adresses[0].ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
