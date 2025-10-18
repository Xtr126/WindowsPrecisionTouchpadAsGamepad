using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Gamepad.Touchpad
{
    public class TouchpadTcpSender : IDisposable
    {
        private TcpClient _tcpClient;
        private readonly IPEndPoint _remoteEndpoint;
        private NetworkStream _networkStream;
        private BinaryWriter _binaryWriter;
        private bool _isConnected = false;

        public TouchpadTcpSender(string remoteIp, int remotePort)
        {
            _tcpClient = new TcpClient();
            _tcpClient.SendBufferSize = 294; // 4 + 29 * 10
            _remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
        }

        
        public void Connect()
        {
            if (_isConnected)
                return;

            try
            {
                _tcpClient.Connect(_remoteEndpoint);
                _networkStream = _tcpClient.GetStream();
                _binaryWriter = new BinaryWriter(_networkStream);
                _isConnected = true;
            }
            catch (SocketException ex)
            {
                MessageBox.Show(
                    $"Unable to connect to server:\n{ex.Message}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        public void Disconnect()
        {
            if (!_isConnected) return;
            _binaryWriter?.Close();
            _networkStream?.Close();
            _tcpClient?.Close();
            _isConnected = false;
        }

        private void WriteTouchpadContactFrame(BinaryWriter bw, TouchpadContact[] contacts)
        {
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
            if (!_isConnected) return;

            try
            {
                WriteTouchpadContactFrame(_binaryWriter, contacts);
            }
            catch (IOException)
            {
                // Connection lost, attempt to reconnect
                _isConnected = false;
                throw;
            }
        }

        public void Dispose()
        {
            Disconnect();
            
            _binaryWriter?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}