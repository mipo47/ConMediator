using System;
using System.Collections.Generic;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using ConMediator.Connector;
using ConMediator.Listen;
using System.Diagnostics;

namespace ConMediator
{
	public delegate void NewEvent(string message);
	public delegate void DataSent(IConnection from, IConnection to, byte[] buffer, int offset, int size);
	public delegate void ErrorException(string error, Exception exc);

	public class Mediator
	{
		public static readonly int PACKET_SIZE = SystemSettings.PacketSize;

		IListen listen;
		ConnectionInfo conInfo;
		Dictionary<IConnection, IConnection> pipes = new Dictionary<IConnection, IConnection>();

		int bytesPerSecond = int.MaxValue;
		public int BytesPerSecond
		{
			get { return bytesPerSecond; }
			set { bytesPerSecond = value; }
		}

		public event NewEvent OnNewEvent;
		public event DataSent OnDataSent;
		public event ErrorException OnError;

		public void Start(ListenType listenType, string listenString, ConnectionType connectionType, string connectionString)
		{
			conInfo = new ConnectionInfo { ConnectionType = connectionType, ConnectionString = connectionString };

			listen = Listen.Listen.CreateListen(listenType);
			listen.Connected += Working;
			listen.Start(listenString);

			if (OnNewEvent != null) if (OnNewEvent != null) OnNewEvent("Listening started: " + listenString);
		}

		public void Stop()
		{
			lock (pipes)
			{
				foreach (IConnection connection in pipes.Keys)
					connection.Close();
				pipes.Clear();
			}

			listen.Stop();
			if (OnNewEvent != null) OnNewEvent("Listening stopped");
		}

		void Working(IConnection sourceConnection)
		{
			if (OnNewEvent != null) OnNewEvent("Client connected " + sourceConnection.Handle);

			if (sourceConnection is HttpConnection)
			{
				HttpConnection c = sourceConnection as HttpConnection;
				c.ModifyDestination(conInfo);
				if (OnNewEvent != null) OnNewEvent("Reconnected to " + conInfo);
			}

			IConnection destinationConnection = Connection.CreateConnection(conInfo.ConnectionType);

			sourceConnection.Event += OnNewEvent;
			sourceConnection.Error += OnError;
			destinationConnection.Event += OnNewEvent;
			destinationConnection.Error += OnError;

			pipes.Add(sourceConnection, destinationConnection);
			pipes.Add(destinationConnection, sourceConnection);

			sourceConnection.DataReceived += Received;
			destinationConnection.DataReceived += Received;

			try
			{
				destinationConnection.Connect(conInfo.ConnectionString);
				sourceConnection.StartReceiving();
				destinationConnection.StartReceiving();
			}
			catch (SocketException exc)
			{
				pipes.Remove(sourceConnection);
				pipes.Remove(destinationConnection);
				if (OnNewEvent != null) OnNewEvent(exc.Message);
			}

			if (OnNewEvent != null) OnNewEvent("Connected to source");

			while (sourceConnection.Connected && destinationConnection.Connected)
				Thread.Sleep(100);

			if (sourceConnection.Connected)
				sourceConnection.Close();

			if (destinationConnection.Connected)
				destinationConnection.Close();

			lock (pipes)
			{
				if (pipes.ContainsKey(sourceConnection))
					pipes.Remove(sourceConnection);

				if (pipes.ContainsKey(destinationConnection))
					pipes.Remove(destinationConnection);
			}
			sourceConnection.DataReceived -= Received;
			destinationConnection.DataReceived -= Received;

			if (OnNewEvent != null) OnNewEvent("Client disconnected");
		}

		void Received(IConnection from, byte[] buffer, int size)
		{
			try
			{
				IConnection to = pipes[from];

				for (int packet = 0; packet < size / PACKET_SIZE; packet++)
				{
					int offset = packet * PACKET_SIZE;
					int length = PACKET_SIZE;
					if (offset + length > size)
						length = size - offset;

					DateTime start = DateTime.Now;

					to.Send(buffer, offset, length);
					if (OnDataSent != null)
						OnDataSent(from, to, buffer, offset, length);

					int millis = (int)(DateTime.Now - start).TotalMilliseconds;
					millis = (length * 1000) / bytesPerSecond - millis;
					if (millis > 5)
						Thread.Sleep(millis);
				}

				if (size % PACKET_SIZE != 0)
				{
					int length = size % PACKET_SIZE;
					int offset = size - length;

					DateTime start = DateTime.Now;

					to.Send(buffer, offset, length);
					if (OnDataSent != null)
						OnDataSent(from, to, buffer, offset, length);

					int millis = (int)(DateTime.Now - start).TotalMilliseconds;
					millis = (length * 1000) / bytesPerSecond - millis;
					if (millis > 5)
						Thread.Sleep(millis);
				}
			}
			catch (KeyNotFoundException exc)
			{
				if (OnError != null) OnError("Connection is not properly closed", exc);
			}
			catch (Exception exc)
			{
				if (OnError != null) OnError("Receiving error", exc);
			}
		}
	}
}
