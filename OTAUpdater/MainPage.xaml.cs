using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OTAUpdater.OTA;
using Xamarin.Forms;

namespace OTAUpdater
{
    public partial class MainPage : ContentPage
    {
        private readonly string _firmwareResource = "OTAUpdater.firmware.aben_2014.bin";
        private readonly OTAUpdateClient _updater = new OTAUpdateClient();

        public MainPage()
        {
            InitializeComponent();
        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            Console.WriteLine("starting update...");
            _updater.UploadFirmware("aben-master.local", 8266, ReadFirmwareFile(_firmwareResource));
            Console.WriteLine("firmware installed!");
        }


        byte[] ReadFirmwareFile(string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resource);
            var data = new byte[stream.Length];

            stream.Read(data, 0, (int)stream.Length);
            return data;
        }
    }
}
