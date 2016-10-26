using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace ConMediator.Connector
{
	class TcpConnection : Connection
	{
		byte[] buffer = new byte[BUFFER_SIZE];
		Socket connection;

		public override ConnectionType ConnectionType { get { return ConnectionType.TCP; } }

		public override string Handle
		{
			get { return connection.Handle.ToString(); }
		}

		public override bool Connected
		{
			get
			{
				if (connection == null)
					return false;

				if (!connection.Connected)
					return false;

				if (connection.Available > 0)
					return true;

				try
				{
					return !connection.Poll(1000, SelectMode.SelectRead);
				}
				catch {}

				return false;
			}
		}

		public TcpConnection() { }

		public TcpConnection(Socket connection)
		{
			this.connection = connection;
		}

		public override void Connect(string connectionString)
		{
			if (connection != null)
			{
				try
				{
					if (this.connection.Connected)
						this.connection.Disconnect(true);
					this.connection.Close();
				}
				catch (SocketException) { }
			}

			
			connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			string[] parts = connectionString.Split(':');
			connection.Connect(parts[0], int.Parse(parts[1]));
		}

		public override void Close()
		{
			// Asinchronous connection closing
			new Thread(() => { connection.Close(10000); }).Start();
		}

		public override void StartReceiving()
		{
			connection.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Received, null);
		}

		public override void Send(byte[] buffer, int offset, int count)
		{
			try
			{
				connection.Send(buffer, offset, count, SocketFlags.None);
			}
			catch (Exception exc)
			{
				Trace.Write(exc.Message);
			}
		}

		void Received(IAsyncResult result)
		{
			try
			{
				int size = connection.EndReceive(result);
				if (size <= 0)
					return;

				onDataReceived(buffer, size);
				connection.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, Received, null);
			}
			catch
			{
				try { connection.Close(); }
				catch { }
			}
		}
	}
}
