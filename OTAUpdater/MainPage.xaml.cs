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
        private readonly string _firmwareResource = "OTAUpdater.firmware.aben_2021.bin";
        private readonly OTAUpdateClient _updater = new OTAUpdateClient();

        public MainPage()
        {
            InitializeComponent();
            activityIndicator.IsVisible = false;

            _updater.MessageLogged += (object sender, string e) =>
            {
                DisplayLog(e);
                Debug.Write(e);
            };
        }

        void DisplayLog(string message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                statusLabel.Text = message;
            });
        }

        void Handle_Clicked(object sender, System.EventArgs e)
        {
            DisplayLog("starting update...");
            activityIndicator.IsVisible = true;

            Task.Run(() =>
            {
                try
                {
                    _updater.UploadFirmware("aben-master.local", 8266, "bildspur", ReadFirmwareFile(_firmwareResource));
                    DisplayLog("firmware installed!");
                }
                catch(Exception ex)
                {
                    DisplayLog($"E: {ex.Message}");
                }
                finally
                {
                    activityIndicator.IsVisible = false;
                }
            });
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
