using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RawInput.Touchpad
{
    public class TouchpadUdpSender : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _remoteEndpoint;
        private readonly BinaryWriter _binaryWriter;
        private readonly MemoryStream _memoryStream;

        public TouchpadUdpSender(string remoteIp, int remotePort)
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SendBufferSize = 8192; // optional tuning
            _remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            _memoryStream = new MemoryStream();
            _binaryWriter = new BinaryWriter(_memoryStream);

        }

        private byte[] SerializeContacts(TouchpadContact[] contacts)
        {
            // Reset the position to the beginning for reading
            _memoryStream.Position = 0;
            _binaryWriter.Write(contacts.Length);
            foreach (var c in contacts)
                c.WriteTo(_binaryWriter);
            return _memoryStream.ToArray();
        }

        public void SendContacts(TouchpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0)
                return;

            var data = SerializeContacts(contacts);
            _udpClient.Send(data, data.Length, _remoteEndpoint);
        }

        public void Dispose()
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            
            _binaryWriter?.Close();
            _binaryWriter?.Dispose();
        
            _memoryStream?.Close();
            _memoryStream?.Dispose();
        }
    }
}