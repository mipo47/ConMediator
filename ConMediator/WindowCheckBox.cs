using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ConMediator
{
	class WindowCheckBox : CheckBox, IDisposable
	{
		Window window;
		System.Threading.Timer timer;

		public WindowCheckBox(Window window, string description)
		{
			this.window = window;
			IsChecked = true;
			Content = description;
			Checked += (object sender, RoutedEventArgs e) => window.Show();
			Unchecked += new RoutedEventHandler(WindowCheckBox_Unchecked);
			window.Closing += window_Closing;
		}

		public void Dispose()
		{
			window.Closing -= window_Closing;
			window.Close();
		}

		void WindowCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (IsChecked == true)
			{
				IsChecked = false;
				return;
			}
			window.Hide();
		}

		void HideWindow(object o)
		{
			timer.Dispose();
			timer = null;

			Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal,
				new RoutedEventHandler(WindowCheckBox_Unchecked),
				null, null);
		}

		void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;

			timer = new System.Threading.Timer(HideWindow, null, 200, System.Threading.Timeout.Infinite);
		}
	}
}
