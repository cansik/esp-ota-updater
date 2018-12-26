using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Xamarin.Forms;

namespace OTAUpdater.OTA
{
    public class OTAUpdateClient
    {
        private readonly Socket _dataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly Socket _commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public event EventHandler<string> MessageLogged;

        public OTAUpdateClient()
        {
            _dataSocket.ReceiveTimeout = 10 * 1000;
            _commandSocket.ReceiveTimeout = 10 * 1000;

            _dataSocket.NoDelay = true;
        }

        void Log(string message, bool newLine = true)
        {
            var msg = message;
            if (newLine)
                msg += Environment.NewLine;
            MessageLogged(this, msg);
        }

        public void UploadFirmware(string remoteAddress, int remotePort, string password, byte[] firmwareData, int localPort = 0)
        {
            var localAddress = IPAddress.Parse(DependencyService.Get<IIPAddressManager>().GetIPAddress());
            Log($"{localAddress}");

            // create response socket
            Log("starting response socket...");
            var localEndpoint = new IPEndPoint(localAddress, localPort);
             _dataSocket.Bind(localEndpoint);
            _dataSocket.Listen(1);

            var ep = _dataSocket.LocalEndPoint as IPEndPoint;
            Log($"bound to {ep}");

            // sending invitation to device
            var inviteMessage = $"{(int)OTACommand.FLASH} {ep.Port} {firmwareData.Length} {firmwareData.MD5Hash()}\n";
            //Log($"invite message: \"{inviteMessage.Trim()}\"");
            var remoteIP = Dns.GetHostAddresses(remoteAddress).FirstOrDefault();
            Log($"remote ip: {remoteIP}");
            var remoteEndPoint = new IPEndPoint(remoteIP, remotePort);

            Log("sending invitation...");
            _commandSocket.SendTo(inviteMessage.EncodeUTF8(), remoteEndPoint);

            // reading response
            var inviteAnswer = _commandSocket.ReceiveBuffer(remoteEndPoint, 128).DecodeUTF8().Clean(); ;
            Log($"invite answer: {inviteAnswer}");

            // check response
            if (!inviteAnswer.StartsWith("AUTH", StringComparison.Ordinal))
            {
                Log("wrong answer!");
                return;
            }

            // prepare authentication
            Log("prepare authentication...");
            var nonce = inviteAnswer.Split(" ".ToCharArray())[1];
            var fileName = "firmware.bin";
            var conceText = $"{fileName}{firmwareData.Length}{firmwareData.MD5Hash()}{remoteAddress}";

            var conce = conceText.EncodeUTF8().MD5Hash();
            var passwordHash = password.EncodeUTF8().MD5Hash();

            var resultText = $"{passwordHash}:{nonce}:{conce}";
            var resultHash = resultText.EncodeUTF8().MD5Hash();
            var authMessage = $"{(int)OTACommand.AUTH} {conce} {resultHash}\n";

            _commandSocket.SendTo(authMessage.EncodeUTF8(), remoteEndPoint);

            // read auth result
            var authAnswer = _commandSocket.ReceiveBuffer(remoteEndPoint, 32).DecodeUTF8().Clean();
            Log($"auth answer: {authAnswer}");

            if (authAnswer != "OK")
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
            connection.NoDelay = true;
            connection.SendTimeout = 60 * 1000;
            connection.ReceiveTimeout = 60 * 1000;

            var chunkSize = 1460;
            var chunks = firmwareData.ToList().ChunkBy(chunkSize).Select(e => e.ToArray()).ToList();
            var chunkCount = 0;
            var transmitOK = false;
            foreach (var chunk in chunks)
            {
                Log($"sending chunk {chunkCount} of {chunks.Count}...");
                var sent = connection.Send(chunk);

                // receive transfer answe
                var transferAnswer = connection.ReceiveBuffer(remoteEndPoint, 32).DecodeUTF8().Clean();
                transmitOK = transferAnswer.Contains("O");

                chunkCount++;
            }

            // check if transfer is finished
            Log("waiting for transfer completed..", false);
            while (!transmitOK)
            {
                var transferAnswer = connection.ReceiveBuffer(remoteEndPoint, 32).DecodeUTF8().Clean();
                transmitOK = transferAnswer.Contains("O");
                Log(".", false);
            }

            Log("");
            Log("transfer finished!");

            // close sockets
            connection.Close();
            _dataSocket.Close();
        }
    }
}
