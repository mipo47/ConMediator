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
using System.Threading;
using System.Net.Sockets;
using ConMediator.Connector;

namespace ConMediator
{
	/// <summary>
	/// Interaction logic for DataWindow.xaml
	/// </summary>
	public partial class DataWindow : Window
	{
		Mediator mediator;
		IConnection fromConnection, toConnection;

		Paragraph paragraph, hexParagraph;
		Brush currentColor = Brushes.Red;

		int start = 0;

		public DataWindow(Mediator mediator)
		{
			InitializeComponent();
			this.mediator = mediator;
			mediator.OnDataSent += new DataSent(mediator_OnDataSent);

			EncodingCombo.Items.Add("None");
			EncodingCombo.Items.Add(Encoding.ASCII);
			EncodingCombo.Items.Add(Encoding.UTF7);
			EncodingCombo.Items.Add(Encoding.UTF8);
			EncodingCombo.Items.Add(Encoding.UTF32);
			EncodingCombo.Items.Add(Encoding.Unicode);
			EncodingCombo.Items.Add(Encoding.Default);

			EncodingCombo.SelectedIndex = 1;
		}

		void mediator_OnDataSent(IConnection from, IConnection to, byte[] buffer, int offset, int size)
		{
			if (!IsVisible)
				return;

			if (Dispatcher.Thread != Thread.CurrentThread)
			{
				Dispatcher.Invoke(
					System.Windows.Threading.DispatcherPriority.Normal,
					new DataSent(mediator_OnDataSent),
					from, to, buffer, offset, size
				);
				return;
			}

			if (from != fromConnection || to != toConnection)
			{
				start = 0;
				paragraph = new Paragraph();
				paragraph.Foreground = Brushes.Black;
				paragraph.Background = Brushes.LightGray;
				paragraph.Inlines.Add(
					DateTime.Now.ToString() + "  ["
					+ from.Handle + "] -> ["
					+ to.Handle + "]"
					);
				DataRichText.Document.Blocks.Add(paragraph);

				if (currentColor == Brushes.Red)
					currentColor = Brushes.Blue;
				else
					currentColor = Brushes.Red;

				if (HexCheck.IsChecked != null && HexCheck.IsChecked == true)
				{
					hexParagraph = new Paragraph();
					hexParagraph.Foreground = Brushes.Black;
					DataRichText.Document.Blocks.Add(hexParagraph);
				}
				else
					hexParagraph = null;

				if (EncodingCombo.SelectedIndex != 0)
				{
					paragraph = new Paragraph();
					paragraph.Foreground = currentColor;
					DataRichText.Document.Blocks.Add(paragraph);
				}
			}

			Encoding encoding = EncodingCombo.SelectedValue as Encoding;
			if (encoding != null)
			{
				string str = encoding.GetString(buffer, offset, size);
				if (NumberCheck.IsChecked == true)
					str = InsertNumbers(str, 1, start);
				paragraph.Inlines.Add(str);
			}

			if (hexParagraph != null && HexCheck.IsChecked == true)
			{
				string str = BytesToHexString(buffer, offset, size);
				if (NumberCheck.IsChecked == true)
					str = InsertNumbers(str, 3, start);
				hexParagraph.Inlines.Add(str);
			}
			
			DataRichText.CaretPosition = DataRichText.CaretPosition.DocumentEnd;
			DataRichText.ScrollToEnd();

			fromConnection = from;
			toConnection = to;
			start += size;
		}

		public static string InsertNumbers(string source, int step, int start)
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i <= source.Length - step; i += step)
			{
				builder.Append('(');
				builder.Append(start++);
				builder.Append(')');
				builder.Append(source.Substring(i, step));
			}
			return builder.ToString();
		}

		public static string BytesToHexString(byte[] bytes, int offset, int size)
		{
			StringBuilder builder = new StringBuilder(size * 3);
			for (int i = offset; i < offset + size; i++)
			{
				if (bytes[i] <= 0xF)
					builder.Append('0');
				builder.Append(Convert.ToString((short)bytes[i], 16));
				builder.Append(' ');
			}
			return builder.ToString();
		}
	}
}
