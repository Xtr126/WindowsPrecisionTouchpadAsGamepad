using System;
using System.Net;
using System.Windows;

namespace Gamepad.Touchpad
{
    public partial class TcpDeviceConnectDialog : Window
    {
        public IPAddress IpAddress { get; private set; }
        public int Port { get; private set; }

        public TcpDeviceConnectDialog()
        {
            InitializeComponent();

            // Set defaults
            IpAddressTextBox.Text = "192.168.1.";
            PortTextBox.Text = "5555";

            Loaded += TcpDeviceConnectDialog_Loaded;
        }

        private void TcpDeviceConnectDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Place cursor at end of IP text for quick entry
            IpAddressTextBox.Focus();
            IpAddressTextBox.CaretIndex = IpAddressTextBox.Text.Length;
        }


        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IPAddress.TryParse(IpAddressTextBox.Text.Trim(), out IPAddress ip))
            {
                MessageBox.Show("Invalid IP address.",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) ||
                port < 1 || port > 65535)
            {
                MessageBox.Show("Invalid port number (1–65535).",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            IpAddress = ip;
            Port = port;

            DialogResult = true;
            Close();
        }
    }
}