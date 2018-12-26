using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using OTAUpdater.OTA;

namespace OTAUpdater.CLI
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("OTA Updater");
            new MainClass().Run();
        }

        private readonly string _firmwareResource = "firmware/aben_2015.bin";
        private readonly OTAUpdateClient _updater = new OTAUpdateClient();

        public void Run()
        {
            Debug.WriteLine("starting update...");

            // read resource
            var data = File.ReadAllBytes(_firmwareResource);

            _updater.MessageLogged += (object sender, string e) =>
            {
                Debug.Write(e);
            };
            _updater.UploadFirmware("aben-master.local", 8266, "bildspur", data);

            Debug.WriteLine("firmware installed!");
        }
    }
}
