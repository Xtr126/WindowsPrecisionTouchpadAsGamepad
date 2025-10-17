using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net;
using System.Collections.ObjectModel;

namespace Gamepad.Touchpad
{
	public partial class MainWindow : Window
	{
		public bool TouchpadExists
		{
			get { return (bool)GetValue(TouchpadExistsProperty); }
			set { SetValue(TouchpadExistsProperty, value); }
		}
		public static readonly DependencyProperty TouchpadExistsProperty =
			DependencyProperty.Register("TouchpadExists", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

		public MainWindow()
		{
			InitializeComponent();
		}

		private HwndSource _targetSource;
		private readonly List<string> _log = new();
		private readonly BinaryWriter _binaryWriter = new BinaryWriter(Console.OpenStandardOutput());
        private TouchpadUdpSender _udpSender = new TouchpadUdpSender("127.0.0.1", 5050);
        private TouchpadTcpSender _tcpSender = new TouchpadTcpSender("127.0.0.1", 6060);
        private bool _stdoutEnabled = false;
        private bool _udpEnabled = false;
        private bool _tcpEnabled = false;
        private string _ipAddress = "127.0.0.1";
        private int _udpPort = 5050;
        private int _tcpPort = 6060;
        private ObservableCollection<TouchpadContactView> _contacts =
            new ObservableCollection<TouchpadContactView>();
        private bool _showContacts = true;
        private List<AdbDevice> _devices = new();


		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			ContactsGrid.ItemsSource = _contacts;

			_targetSource = PresentationSource.FromVisual(this) as HwndSource;
			_targetSource?.AddHook(WndProc);

			TouchpadExists = TouchpadHelper.Exists();

			_log.Add($"Precision touchpad exists: {TouchpadExists}");

			if (TouchpadExists)
			{
				var success = TouchpadHelper.RegisterInput(_targetSource.Handle);

				_log.Add($"Precision touchpad registered: {success}");
			}
			
		}

		public void UpdateContacts(TouchpadContact[] newContacts)
        {	
            // Clear and repopulate the observable collection
            _contacts.Clear();
            foreach (var view in TouchpadContactView.FromContacts(newContacts))
            {
                _contacts.Add(view);
            }
        }

		static void WriteTouchpadContactFrame(BinaryWriter bw, TouchpadContact[] contacts)
		{
			// Calculate frame size: count (4) + contacts (29 * N)
			int frameBodySize = 4 + contacts.Length * 29;
			bw.Write(frameBodySize);
			bw.Write(contacts.Length);
			foreach (var c in contacts)
				c.WriteTo(bw);
			bw.Flush();
		}
		
		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg)
			{
				case TouchpadHelper.WM_INPUT:
					var contacts = TouchpadHelper.ParseInput(lParam);
					
					if (_stdoutEnabled) 
						WriteTouchpadContactFrame(_binaryWriter, contacts);

					if (_tcpEnabled)
						_tcpSender.SendContacts(contacts);
						
					if (_udpEnabled)
						_udpSender.SendContacts(contacts);

					if (_showContacts && ContactsContent.Visibility == Visibility.Visible)
						UpdateContacts(contacts);
					break;
			}
			return IntPtr.Zero;
		}

		private void IpAddressBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string newIp = IpAddressBox.Text.Trim();

            if (IPAddress.TryParse(newIp, out _))
            {
                _ipAddress = newIp;
                IpAddressBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
				_udpSender = new TouchpadUdpSender(_ipAddress, _udpPort);
				_tcpSender = new TouchpadTcpSender(_ipAddress, _tcpPort);
            }
            else
            {
                // Show a red border if invalid
                IpAddressBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
        }

        private void UdpPortBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(UdpPortBox.Text, out int port) && port > 0 && port <= 65535)
            {
                _udpPort = port;
                UdpPortBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
				_udpSender = new TouchpadUdpSender(_ipAddress, _udpPort);
            }
            else
            {
                UdpPortBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
        }

		// Called when TCP Port textbox content changes
		private void TcpPortBox_TextChanged(object sender, TextChangedEventArgs e)
		{
            if (int.TryParse(TcpPortBox.Text, out int port) && port > 0 && port <= 65535)
            {
                _tcpPort = port;
                TcpPortBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
				_tcpSender = new TouchpadTcpSender(_ipAddress, _tcpPort);
            }
            else
            {
                TcpPortBox.BorderBrush = System.Windows.Media.Brushes.Red;
            }
		}

        private void StdoutEnableSwitch_Checked(object sender, RoutedEventArgs e)
        {
            _stdoutEnabled = true;
        }

        private void StdoutEnableSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            _stdoutEnabled = false;
        }

		// Called when UDP enable checkbox is checked
		private void UdpEnableSwitch_Checked(object sender, RoutedEventArgs e)
		{
            _udpEnabled = true;
		}

		// Called when UDP enable checkbox is unchecked
		private void UdpEnableSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			_udpEnabled = false;
		}

		// Called when TCP enable checkbox is checked
		private void TcpEnableSwitch_Checked(object sender, RoutedEventArgs e)
		{
			TcpConnectButton.IsEnabled = true;
			_tcpEnabled = true;
		}

		// Called when TCP enable checkbox is unchecked
		private void TcpEnableSwitch_Unchecked(object sender, RoutedEventArgs e)
		{
			TcpConnectButton.IsEnabled = false;
			_tcpEnabled = false;
		}
		
		private void TCP_Connect(object sender, RoutedEventArgs e)
		{
			_tcpSender.Connect();
		}

		// Called when show contacts checkbox is checked
		private void ShowContacts_Checked(object sender, RoutedEventArgs e)
		{
			_showContacts = true;
		}

		// Called when show contacts checkbox is unchecked
		private void ShowContacts_Unchecked(object sender, RoutedEventArgs e)
		{
			_showContacts = false;
		}

		private void IconListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Hide all
			DevicesContent.Visibility = Visibility.Collapsed;
			DataOutContent.Visibility = Visibility.Collapsed;
			ContactsContent.Visibility = Visibility.Collapsed;

			// Show the selected one
			switch (IconListView.SelectedIndex)
			{
				case 0:
					DevicesContent.Visibility = Visibility.Visible;
					break;
				case 1:
					DataOutContent.Visibility = Visibility.Visible;
					break;
				case 2:
					ContactsContent.Visibility = Visibility.Visible;
					break;
			}
		}

        private void ListDevices_Click(object sender, RoutedEventArgs e)
        {
            _devices.Clear();

            if (AdbHelper.GetConnectedDevices(_devices))
            {
                DevicesGrid.ItemsSource = null; // Refresh
                DevicesGrid.ItemsSource = _devices;
            }
            else
            {
                MessageBox.Show("No devices found.", "ADB Devices", MessageBoxButton.OK, MessageBoxImage.Information);
                DevicesGrid.ItemsSource = null;
            }
        }
	}
}