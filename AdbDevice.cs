namespace RawInput.Touchpad
{

    internal class AdbDevice
    {
        public string Serial { get; set; }
        public string State { get; set; }
        public string Model { get; set; }
        public string Product { get; set; }
        public string Device { get; set; }
        public string TransportId { get; set; }
        public bool Selected { get; set; }
        
        public AdbDevice()
        {
            Selected = false;
        }
    }
}