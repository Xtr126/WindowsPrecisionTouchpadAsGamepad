using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RawInput.Touchpad
{
    public class TouchpadTcpSender : IDisposable
    {
        private TcpClient _tcpClient;
        private readonly IPEndPoint _remoteEndpoint;
        private NetworkStream _networkStream;
        private readonly BinaryWriter _binaryWriter;
        private readonly MemoryStream _memoryStream;
        private bool _isConnected = false;

        public TouchpadTcpSender(string remoteIp, int remotePort)
        {
            _tcpClient = new TcpClient();
            _tcpClient.SendBufferSize = 8192;
            _remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            _memoryStream = new MemoryStream();
            _binaryWriter = new BinaryWriter(_memoryStream);
        }

        public void Connect()
        {
            if (_isConnected) return;
            
            _tcpClient.Connect(_remoteEndpoint);
            _networkStream = _tcpClient.GetStream();
            _isConnected = true;
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            
            _networkStream?.Close();
            _tcpClient?.Close();
            _isConnected = false;
        }

        private void WriteTouchpadContactFrame(BinaryWriter bw, TouchpadContact[] contacts)
        {
            // Reset the memory stream position for writing
            _memoryStream.Position = 0;
            
            // Calculate frame size: count (4) + contacts (29 * N)
            int frameBodySize = 4 + contacts.Length * 29;
            bw.Write(frameBodySize);
            bw.Write(contacts.Length);
            foreach (var c in contacts)
                c.WriteTo(bw);
        }

        public void SendContacts(TouchpadContact[] contacts)
        {
            if (contacts == null || contacts.Length == 0)
                return;

            // Ensure we're connected before sending
            if (!_isConnected)
                Connect();

            try
            {
                WriteTouchpadContactFrame(_binaryWriter, contacts);
                var data = _memoryStream.ToArray();
                _networkStream.Write(data, 0, (int)_memoryStream.Position);
                _networkStream.Flush();
            }
            catch (IOException)
            {
                // Connection lost, attempt to reconnect on next send
                _isConnected = false;
                throw;
            }
        }

        public void Dispose()
        {
            Disconnect();
            
            _tcpClient?.Dispose();
            _networkStream?.Dispose();
            _binaryWriter?.Close();
            _binaryWriter?.Dispose();
            _memoryStream?.Close();
            _memoryStream?.Dispose();
        }
    }
}