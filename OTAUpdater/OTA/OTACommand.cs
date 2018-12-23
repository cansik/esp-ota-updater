using System;
namespace OTAUpdater.OTA
{
    [Flags]
    public enum OTACommand
    {
        FLASH = 0,
        SPIFFS = 100,
        AUTH = 200
    }
}
