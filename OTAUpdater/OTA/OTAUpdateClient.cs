using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace OTAUpdater.OTA
{
    public class OTAUpdateClient
    {
        private readonly Socket _dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly Socket _commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public OTAUpdateClient()
        {
            _dataSocket.ReceiveTimeout = 10 * 1000;
            _commandSocket.ReceiveTimeout = 10 * 1000;
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
            _dataSocket.Bind(localEndpoint);
            _dataSocket.Listen(1);

            // sending invitation to device
            Log("sending invitation...");
            var inviteMessage = $"{OTACommand.FLASH} {localPort} {firmware.Length} {firmware.ToHex(false)}\n";
            Log($"message: \"{inviteMessage.Trim()}\"");
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort);
            _commandSocket.SendTo(inviteMessage.EncodeUTF8(), remoteEndPoint);

            // reading response
            var inviteAnswer = _commandSocket.ReceiveBuffer(128).DecodeUTF8();
            Log($"invite answer: {inviteAnswer}");

            // check response
            if(!inviteAnswer.StartsWith("AUTH", StringComparison.Ordinal))
            {
                Log("wrong answer!");
                return;
            }

            // prepare authentication
            Log("prepare authentication...");
            var nonce = inviteAnswer.Split(" ".ToCharArray())[1];
            var fileName = "firmware.bin";
            var conceText = $"{fileName}{firmware.Length}{firmware.ToHex(false)}{remoteAddress}";

            var conce = conceText.EncodeUTF8().ToHex(false);
            var passwordHash = password.EncodeUTF8().ToHex(false);

            var resultText = $"{passwordHash}:{nonce}:{conce}";
            var resultHash = resultText.EncodeUTF8().ToHex(false);
            var authMessage = $"{OTACommand.AUTH} {conce} {resultHash}\n";

            _commandSocket.SendTo(authMessage.EncodeUTF8(), remoteEndPoint);

            // read auth result
            var authAnswer = _commandSocket.ReceiveBuffer(32).DecodeUTF8();
            Log($"auth answer: {inviteAnswer}");

            if(authAnswer != "OK")
            {
                Log("authentication failed!");
                return;
            }

            Log("authentication correct!");
            _commandSocket.Close();

            Log("waiting for device...", false);
            var connection = _dataSocket.Accept();
            Log($"device connected [{connection.RemoteEndPoint}]");

            // send data


            // close sockets
            _dataSocket.Close();
        }
    }
}
