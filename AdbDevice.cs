namespace Gamepad.Touchpad
{
    internal class AdbDevice
    {
        public string Device { get; set; }
        public string Serial { get; set; }
        public bool Streaming { get; set; }
        public bool Installed { get; set; }
        public string State { get; set; }
        public string Model { get; set; }
        public string Product { get; set; }
        public string TransportId { get; set; }
        
        public AdbDevice()
        {
            Streaming = false;
            Installed = AdbPushServer.IsServerInstalled(Serial);
        }
    }
}