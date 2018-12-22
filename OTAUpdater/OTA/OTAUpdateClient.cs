using System;
using System.Net.Sockets;
using System.Reflection;

namespace OTAUpdater.OTA
{
    public class OTAUpdateClient
    {
        private readonly Socket _pingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly Socket _commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public OTAUpdateClient()
        {
        }

        public void UploadFirmware(string deviceAddress, int devicePort, byte[] firmware)
        {
            Console.WriteLine($"Size: {firmware.Length}");
        }
    }
}
