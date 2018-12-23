using System;
using System.Diagnostics;
using System.IO;
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

            _dataSocket.NoDelay = true;
        }

        void Log(string message, bool newLine = true)
        {
            Debug.Write(message);
            if (newLine)
                Debug.Write(Environment.NewLine);
        }

        public void UploadFirmware(int localPort, string remoteAddress, int remotePort, string password, byte[] firmwareData)
        {
            // read local ip address
            Log("reading local ip address...", false);
            var localAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
            if (localAddress == null)
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
            var inviteMessage = $"{(int)OTACommand.FLASH} {localPort} {firmwareData.Length} {firmwareData.MD5Hash()}\n";
            Log($"invite message: \"{inviteMessage.Trim()}\"");
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
