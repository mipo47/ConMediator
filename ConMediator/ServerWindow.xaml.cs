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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.IO;

namespace ConMediator
{
    public delegate void AddSocketDelegate(Socket socket);

	public partial class ServerWindow : Window
	{
		const int tabSize = 4; 
		const int BUFFER_SIZE = 8192;
		const string codeTemplate =
@"using System;
using System.Net.Sockets;

public class AutoAnswer
{
  public string GetAnswer(Socket socket, string input) { return null; }
}";
	
		object autoAnswer;
		MethodInfo getAnswer, getRawAnswer;
		MethodInfo updateOutput, updateRawOutput;

		TcpListener listener;

		Dictionary<Socket, byte[]> buffers = new Dictionary<Socket, byte[]>();
		Dictionary<IntPtr, Socket> sockets = new Dictionary<IntPtr, Socket>();

		string address;
		int port;

		public ServerWindow(int port)
		{
			InitializeComponent();

			this.port = port;
			CodeText.Text = codeTemplate;

			listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			Title = "Listening port " + port;
			listener.BeginAcceptSocket(Working, null);
			CompileButton_Click(this, null);
			this.Closing += (object sender, System.ComponentModel.CancelEventArgs e) => listener.Stop();
		}

		public ServerWindow(string address, int port)
		{
			InitializeComponent();

			this.port = port;
			this.address = address;
			CodeText.Text = codeTemplate;
			CompileButton_Click(this, null);

			Reconnect();
			Title = "Connected to " + address + ':' + port;
		}

		void Reconnect()
		{
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(address, port);

			Thread thread = new Thread(StartSocketReading);
			thread.Start(socket);
		}

        void AddSocket(Socket socket)
        {
			sockets[socket.Handle] = socket;
            SocketCombo.Items.Add(socket.Handle);
			SocketCombo.SelectedIndex = SocketCombo.Items.Count - 1;
        }

		void RemoveSocket(Socket socket)
		{
			sockets.Remove(socket.Handle);
			SocketCombo.Items.Remove(socket.Handle);

			//if (listener == null && SocketCombo.Items.Count == 0)
			//{
			//    MessageBoxResult result = MessageBox.Show("You are disconnected. Reconnect ?", "Connection lost", MessageBoxButton.YesNo, MessageBoxImage.Question);
			//    if (result == MessageBoxResult.Yes)
			//        Reconnect();
			//    else
			//        Close();
			//}
			Close();
		}

		void Working(IAsyncResult result)
		{
			Socket socket;
			try
			{
				socket = listener.EndAcceptSocket(result);
				listener.BeginAcceptSocket(Working, null);
			}
			catch (Exception) { return; }

			StartSocketReading(socket);
		}

		void StartSocketReading(object socketObject)
		{
			Socket socket = socketObject as Socket;

			Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal,
				new AddSocketDelegate(AddSocket),
				socket
			);

			byte[] buffer = new byte[BUFFER_SIZE];
			buffers[socket] = buffer;
			try
			{
				socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Received, socket);
			}
			catch (Exception) { return; }

			while (socket.Connected)
				Thread.Sleep(100);

			if (socket.Connected)
				socket.Close();

			buffers.Remove(socket);

			Dispatcher.Invoke(
				System.Windows.Threading.DispatcherPriority.Normal,
				new AddSocketDelegate(RemoveSocket),
				socket
			);
		}

		void Received(IAsyncResult result)
		{
			try
			{
				Socket socket = result.AsyncState as Socket;
				int lenght = socket.EndReceive(result);
				if (lenght == 0)
					return;

				byte[] buffer = buffers[socket];
				byte[] packet = new byte[lenght];
				Array.Copy(buffer, packet, lenght);

				try
				{
					if (getRawAnswer != null)
					{
						byte[] answer = getRawAnswer.Invoke(autoAnswer, new object[] { socket, packet }) as byte[];
						if (answer != null && answer.Length > 0)
							socket.Send(answer);
					}
					else if (getAnswer != null)
					{
						string input = Encoding.ASCII.GetString(packet);
						object objectAnswer = getAnswer.Invoke(autoAnswer, new object[] { socket, input });
						if (objectAnswer != null)
						{
							string answer = objectAnswer.ToString();
							if (answer != null && answer != string.Empty)
								socket.Send(Encoding.ASCII.GetBytes(answer));
						}
					}
				}
				catch (Exception exc) { MessageBox.Show("Error in class AutoAnswer: " + exc.Message); }

				socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Received, socket);
			}
			catch (ObjectDisposedException) { }
			catch (SocketException) { }
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message);
			}
		}

		private void CompileButton_Click(object sender, RoutedEventArgs e)
		{
			CSharpCodeProvider codeProvider = new CSharpCodeProvider();
			CompilerParameters options = new CompilerParameters(new string[] { "system.dll" });

			CompilerResults results = codeProvider.CompileAssemblyFromSource(options, CodeText.Text);

			if (results.Errors.Count == 0)
			{
				if (autoAnswer != null)
					MessageBox.Show("Compiled");

				autoAnswer = results.CompiledAssembly.CreateInstance("AutoAnswer");
				Type type = autoAnswer.GetType();
				getAnswer = type.GetMethod("GetAnswer");
				getRawAnswer = type.GetMethod("GetRawAnswer");
				updateOutput = type.GetMethod("UpdateOutput");
				updateRawOutput = type.GetMethod("UpdateRawOuput");
				return;
			}

			string errors = "";
			foreach (CompilerError error in results.Errors)
				errors = error + Environment.NewLine;

			MessageBox.Show(errors);
		}

		private void InputText_TextChanged(object sender, TextChangedEventArgs e)
		{
			Socket socket = null;
			if (SocketCombo.SelectedValue != null)
				socket = sockets[(IntPtr)SocketCombo.SelectedValue];

			if (socket == null || InputText.Text == "" || InputText.Text == "\\")
				return;

			string text = InputText.Text;
			if (text.StartsWith("`"))
			{
				if (text.Length >= 2 && text.EndsWith("`"))
					text = text.Substring(1, text.Length - 2);
				else return;

				if (text == "")
					text = "\r\n";
			}

			byte[] buffer;
			if (text.StartsWith("~"))
			{
				if (text.Length >= 2 && text.EndsWith("~"))
					buffer = Common.ConvertToHEX(text.Substring(1, text.Length - 2));
				else return;
			}
			else
			{
                text = text.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\\", "\\").Replace("\\`", "`").Replace("\\~", "~");
				buffer = ASCIIEncoding.ASCII.GetBytes(text);
			}
			InputText.Clear();

			try
			{
				socket.Send(buffer);
			}
			catch (Exception exc) { Title = exc.Message; }
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			foreach (KeyValuePair<IntPtr, Socket> pair in sockets)
				pair.Value.Close();
		}

		private void LoadCode_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = "CodeTemplates";
			if (dialog.ShowDialog(this) == true)
			{
				StreamReader reader = new StreamReader(dialog.OpenFile());
				CodeText.Text = reader.ReadToEnd().Replace("\t", "  ");
				autoAnswer = null;
				CompileButton_Click(this, null);
			}
		}
	}
}
