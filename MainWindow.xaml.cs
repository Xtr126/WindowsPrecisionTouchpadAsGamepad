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

namespace RawInput.Touchpad
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

		public string TouchpadContacts
		{
			get { return (string)GetValue(TouchpadContactsProperty); }
			set { SetValue(TouchpadContactsProperty, value); }
		}
		public static readonly DependencyProperty TouchpadContactsProperty =
			DependencyProperty.Register("TouchpadContacts", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

		public MainWindow()
		{
			InitializeComponent();
		}

		private HwndSource _targetSource;
		private readonly List<string> _log = new();
		private readonly BinaryWriter _binaryWriter = new BinaryWriter(Console.OpenStandardOutput());
        private TouchpadUdpSender _sender = new TouchpadUdpSender("127.0.0.1", 5050);
        private bool _stdoutEnabled = false;

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

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
					else 
						_sender.SendContacts(contacts);
					
					TouchpadContacts = string.Join(Environment.NewLine, contacts.Select(x => x.ToString()));
					break;
			}
			return IntPtr.Zero;
		}

        private string _ipAddress = "127.0.0.1";
        private int _udpPort = 5050;

		private void IpAddressBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string newIp = IpAddressBox.Text.Trim();

            if (IPAddress.TryParse(newIp, out _))
            {
                _ipAddress = newIp;
                IpAddressBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
				_sender = new TouchpadUdpSender(_ipAddress, _udpPort);
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
				_sender = new TouchpadUdpSender(_ipAddress, _udpPort);
            }
            else
            {
                UdpPortBox.BorderBrush = System.Windows.Media.Brushes.Red;
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
	}
}