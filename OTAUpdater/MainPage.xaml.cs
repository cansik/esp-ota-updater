using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            activityIndicator.IsVisible = false;
        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            Debug.WriteLine("starting update...");
            activityIndicator.IsVisible = true;

            _updater.UploadFirmware("aben-master.local", 8266, ReadFirmwareFile(_firmwareResource));

            Debug.WriteLine("firmware installed!");
            activityIndicator.IsVisible = false;
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
