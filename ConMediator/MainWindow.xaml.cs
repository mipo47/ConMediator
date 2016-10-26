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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using ConMediator.Properties;

namespace ConMediator
{
	delegate void MessageDelegate(string message);

	public partial class MainWindow : Window
	{
		Mediator mediator;

		public MainWindow()
		{
			try
			{
				mediator = new Mediator();
				mediator.OnNewEvent += new NewEvent(mediator_OnNewEvent);

				InitializeComponent();

				// Load stored settings
				ListenCombo.SelectedIndex = Settings.Default.ListenType;
				ListenPortText.Text = Settings.Default.ListenPort;
				ConTypeCombo.SelectedIndex = Settings.Default.ConnectionType;
				ConStringText.Text = Settings.Default.Connection;
				SpeedCombo.SelectedIndex = Settings.Default.MaxSpeed;
			}
			catch (Exception exc) 
			{ 
				MessageBox.Show(exc.Message); 
				Close(); 
				return; 
			}
			
			/* Protection
			long applicationCode = 1;
			string serverKey = "PFJTQUtleVZhbHVlPg0KICA8TW9kdWx1cz52cDF1Und3TTJkQ25DZ0E5U3d3VHdEMWlhY1B5bXNFQThEbnA1SDU4eVE1ek5UUnNSa3JkVFo1MjFROTRKR3RkY2g2NlV0bGEyTHFoUnRNeUVjb2JYdytKUEtuWTVhOFhvQW04MFBlWTFyWUJsU1E3K3I0cmM5V1k0Nk13Z1Ura0IyV0Z1T3pMcUtOS28waXJhUHhEaGRrTnEzNHQybDVZVk1tMklzQ3lvQVU9PC9Nb2R1bHVzPg0KICA8RXhwb25lbnQ+QVFBQjwvRXhwb25lbnQ+DQo8L1JTQUtleVZhbHVlPg==";

			SoftwareSecurity.Protector protector = new SoftwareSecurity.Protector(applicationCode, serverKey);

			try
			{
				if (!protector.LoadLicense() || !protector.IsValidVersion)
				{
					MessageBox.Show("Program unlicensed !");
					Close();
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.StackTrace);
				Close();
			}
			*/

			WindowCombo.Items.Add("Visible windows");
			WindowCombo.SelectedIndex = 0;

			Common.Log.Info("Loaded");
		}

		void WriteNewMessage(string message)
		{
			ConsoleText.Text += message + Environment.NewLine;
			ConsoleText.ScrollToEnd();
		}

		void mediator_OnNewEvent(string message)
		{
			Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal, 
				new MessageDelegate(WriteNewMessage), 
				message
			);
		}

		private void StartupButton_Click(object sender, RoutedEventArgs e)
		{
			if (StartupButton.Content.ToString() == "Start")
			{
				StartupButton.Content = "Stop";

				DataWindow dataWindow = new DataWindow(mediator);
				dataWindow.Show();

				WindowCheckBox checkBox = new WindowCheckBox(dataWindow, "Data transfer monitor");
				WindowCombo.Items.Add(checkBox);

				string listenString = ListenPortText.Text;
				string connection = ConStringText.Text;
				int port;
				if (connection.StartsWith("~") && int.TryParse(connection.Substring(1), out port))
				{
					connection = "localhost:" + connection.Substring(1);
					ServerWindow serverWindow = new ServerWindow(port);
					serverWindow.Show();

					checkBox = new WindowCheckBox(serverWindow, "Server emulator");
					WindowCombo.Items.Add(checkBox);
				}

				Listen.ListenType listenType = Listen.ListenType.TCP;
				switch (ListenCombo.SelectedIndex)
				{
					case 0: listenType = Listen.ListenType.TCP; break;
					case 1: listenType = Listen.ListenType.HTTP; break;
				}

				Connector.ConnectionType connectionType = Connector.ConnectionType.TCP;
				switch (ConTypeCombo.SelectedIndex)
				{
					case 0: connectionType = Connector.ConnectionType.TCP; break;
					case 1: connectionType = Connector.ConnectionType.Serial; break;
					case 2: connectionType = Connector.ConnectionType.HTTP; break;
				}

				mediator.Start(listenType, listenString, connectionType, connection);
				Common.Log.Info("Started " + connectionType + connection);
			}
			else
			{
				StartupButton.Content = "Start";

				for (int i = WindowCombo.Items.Count - 1; i > 0; i--)
				{
					WindowCheckBox checkBox = WindowCombo.Items[i] as WindowCheckBox;
					checkBox.Dispose();
					WindowCombo.Items.Remove(checkBox);
				}

				mediator.Stop();
				Common.Log.Info("Stopped");
			}
		}

		private void SpeedCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			mediator.BytesPerSecond = (int)(16.0 * Math.Pow(2, SpeedCombo.SelectedIndex));
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (StartupButton.Content.ToString() == "Stop")
				StartupButton_Click(this, null);

			Settings.Default.ListenType = ListenCombo.SelectedIndex;
			Settings.Default.ListenPort = ListenPortText.Text;
			Settings.Default.ConnectionType = ConTypeCombo.SelectedIndex;
			Settings.Default.Connection = ConStringText.Text;
			Settings.Default.MaxSpeed = SpeedCombo.SelectedIndex; 
			Settings.Default.Save();
		}

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			ConnectionWindow cw = new ConnectionWindow(int.Parse(ListenPortText.Text));
			cw.ShowDialog();
			if (cw.ServerWindow != null)
			{
				this.Closing += (object ss, System.ComponentModel.CancelEventArgs ee) =>
				{
					if (cw.ServerWindow.IsVisible)
						cw.ServerWindow.Close();
				};
			}
			/*
			if (cw.ServerWindow != null)
			{
				WindowCheckBox checkBox = new WindowCheckBox(cw.ServerWindow, "Connection to " + cw.FullAddress);
				WindowCombo.Items.Add(checkBox);
			}
			*/
		}
	}
}
