using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ConMediator
{
	/// <summary>
	/// Interaction logic for ConnectionWindow.xaml
	/// </summary>
	public partial class ConnectionWindow : Window
	{
		public ServerWindow ServerWindow
		{
			get;
			private set;
		}

		public string FullAddress
		{
			get;
			private set;
		}

		public ConnectionWindow(int port)
		{
			InitializeComponent();
			AddressText.Text = "localhost";
			PortText.Text = port.ToString();
		}

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender == ConnectButton)
			{
				ServerWindow = new ServerWindow(AddressText.Text, int.Parse(PortText.Text));
				FullAddress = AddressText.Text + ':' + PortText.Text;
				ServerWindow.Show();
			}

			Close();
		}
	}
}
