using System;
using System.Collections.Generic;
using System.Linq;

namespace RawInput.Touchpad
{
    internal static class AdbDevicesParser
    {
        private const string Header = "List of devices attached";
        
        public static bool ParseDevices(string str, List<AdbDevice> outDevices)
        {
            bool headerFound = false;
            
            if (string.IsNullOrEmpty(str))
                return false;
                
            string[] lines = str.Split('\n');
            
            foreach (string line in lines)
            {
                string trimmedLine = line.TrimEnd('\r');
                
                if (!headerFound)
                {
                    if (trimmedLine.StartsWith(Header))
                    {
                        headerFound = true;
                    }
                    // Skip everything until the header, there might be garbage lines
                    // related to daemon starting before
                    continue;
                }
                
                // Skip empty lines after header
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                    
                AdbDevice device = ParseDevice(trimmedLine);
                if (device != null)
                {
                    outDevices.Add(device);
                }
            }
            
            // If no header was found but we have devices, it might be a different format
            // For robustness, we'll return true if we found any valid devices
            return headerFound || outDevices.Count > 0;
        }
        
        private static AdbDevice ParseDevice(string line)
        {
            // One device line looks like:
            // "0123456789abcdef	device usb:2-1 product:MyProduct model:MyModel "
            //     "device:MyDevice transport_id:1"
            
            if (string.IsNullOrEmpty(line))
                return null;
                
            if (line[0] == '*')
            {
                // Garbage lines printed by adb daemon while starting start with a '*'
                return null;
            }
            
            if (line.StartsWith("adb server"))
            {
                // Ignore lines starting with "adb server":
                //   adb server version (41) doesn't match this client (39); killing...
                return null;
            }
            
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
                return null;
                
            string serial = parts[0];
            string state = parts[1];
            
            var device = new AdbDevice
            {
                Serial = serial,
                State = state
            };
            
            // Parse additional properties for lines with more than 2 parts
            if (parts.Length > 2)
            {
                for (int i = 2; i < parts.Length; i++)
                {
                    string token = parts[i];
                    int colonIndex = token.IndexOf(':');
                    
                    if (colonIndex > 0 && colonIndex < token.Length - 1)
                    {
                        string key = token.Substring(0, colonIndex);
                        string value = token.Substring(colonIndex + 1);
                        
                        switch (key)
                        {
                            case "model":
                                device.Model = value;
                                break;
                            case "product":
                                device.Product = value;
                                break;
                            case "device":
                                device.Device = value;
                                break;
                            case "transport_id":
                                device.TransportId = value;
                                break;
                        }
                    }
                }
            }
            
            return device;
        }
    }
}