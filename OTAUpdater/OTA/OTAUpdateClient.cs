using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace OTAUpdater.OTA
{
    public class OTAUpdateClient
    {
        private readonly Socket _responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly Socket _commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public OTAUpdateClient()
        {
        }

        void Log(string message, bool newLine = true)
        {
            Debug.Write(message);
            if (newLine)
                Debug.Write(Environment.NewLine);
        }

        public void UploadFirmware(int localPort, string remoteAddress, int remotePort, string password, byte[] firmware)
        {
            // read local ip address
            Log("reading local ip address...", false);
            var localAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
            if(localAddress == null)
            {
                Log("could not be retrieved!");
                return;
            }
            Log($"{localAddress}");

            // create response socket
            Log("starting response socket...");
            var localEndpoint = new IPEndPoint(localAddress, localPort);
            _responseSocket.Bind(localEndpoint);
            _responseSocket.Listen(1);

            // sending invitation to device
            Log("sending invitation...");
            var message = $"{OTACommand.FLASH} {localPort} {firmware.Length} {firmware.ToHex(false)}\n";
            Log($"message: \"{message.Trim()}\"");
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
            _commandSocket.SendTo(message.EncodeUTF8(), remoteEndPoint);

            // reading response
            _commandSocket.ReceiveTimeout = 10 * 1000;
            var answer = _commandSocket.ReceiveBuffer(128).DecodeUTF8();
            Log($"answer: {answer}");


            // close sockets
            _responseSocket.Close();
        }
    }
}
