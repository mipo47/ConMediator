using System;
using System.Collections.Generic;

using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace ConMediator.Connector
{
	class HttpConnection : HttpLogin, IConnection
	{
		bool isSource = false;
		string reconnect = null;
		byte[] buffer = new byte[SystemSettings.PacketSize];
		HttpWebRequest webrequest;
		Stream requestStream;
		long written = 0;
		long maxContent = SystemSettings.ContentSize;

		public event DataReceivedDelegate DataReceived;
		public event NewEvent Event;
		public event ErrorException Error;

		public ConnectionType ConnectionType { get { return ConnectionType.HTTP; } }
		public string Handle { get; protected set; }
		public bool Connected {	get; protected set;	}
		public bool StopWaiting { get; set; }

		public HttpConnection(bool isSource)
		{
			this.isSource = isSource;
		}

		#region Connect/Disconnect

		public void Connect(string connectionString)
		{
			try
			{
				Login();
				WaitForContact(connectionString);
			}
			catch (Exception exc)
			{
				if (Error != null) Error("Unable to connect " + this, exc);
			}
			
			if (StopWaiting)
				return;

			if (Event != null) Event("Connected " + this);
			Connected = true;

			StartSending();
		}

		void WaitForContact(string listenPort)
		{
			string type = isSource ? "Source" : "Destination";
			int index = listenPort.IndexOf(' ');
			string reconnectString = "";
			if (index > 0)
			{
				reconnectString = "&Reconnect=" + listenPort.Substring(index + 1);
				listenPort = listenPort.Substring(0, index);
			}

			while (!StopWaiting)
			{
				string result = SendReceive(
					SystemSettings.Host + "Command/Connect?Ticket=" + ticket 
					+ "&ListenPort=" + listenPort + "&type=" + type + reconnectString
				);

				if (result.StartsWith("Ok "))
				{
					index = result.IndexOf(' ', 3);
					if (index < 0)
						Handle = result.Substring(3);
					else
					{	// custom destination
						Handle = result.Substring(3, index - 3);
						reconnect = result.Substring(index + 1);
					}
					return;
				}

				switch (result)
				{
					case "Retry":
						Thread.Sleep(SystemSettings.RetryTimeout);
						break;
					default:
						throw new Exception(result);
				}
			}
		}

		public void Close()
		{
			lock (this)
			{
				if (!Connected)
					return;
				else
					Connected = false;
			}

			string result = string.Empty;
			try
			{
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(SystemSettings.Host + "Command/Close?ID=" + Handle);
				WebResponse response = request.GetResponse();
				StreamReader reader = new StreamReader(response.GetResponseStream());
				result = reader.ReadToEnd();
				if (result != "Ok")
					throw new Exception("Cannot close connection: " + result);

				if (Event != null) Event("Closed " + this);
			}
			catch (Exception exc)
			{
				if (Error != null) Error("Close error: " + this, exc);
			}
		} 
		#endregion

		#region Send/Receive

		void StartSending()
		{
			try
			{
				written = 0;
				Uri uri = new Uri(SystemSettings.Host + "Command/Send?ID=" + Handle + "&tick=" + SystemSettings.GetTick());
				webrequest = (HttpWebRequest)WebRequest.Create(uri);
				webrequest.ContentType = "multipart/form-data";
				webrequest.Method = "POST";
				webrequest.KeepAlive = true;
				webrequest.ContentLength = maxContent;
				requestStream = webrequest.GetRequestStream();
				if (Event != null) Event("Start sending: " + this);
			}
			catch (Exception exc)
			{
				if (Error != null) Error("Sending cannot start: " + this, exc);
			}
		}

		public void Send(byte[] buffer, int offset, int count)
		{
			try
			{
				written += count;
				if (written > maxContent)
				{
					int part = (int)(count - (written - maxContent));
					requestStream.Write(buffer, offset, part);
					StartSending();
					Send(buffer, offset + part, count - part);
				}
				else
				{
					requestStream.Write(buffer, offset, count);
					requestStream.Flush();
				}
				if (Event != null) Event("Sended " + this);
			}
			catch (WebException exc)
			{
				if (Error != null) Error("Send error: " + this, exc);
				if (Connected)
				{
					StartSending();
					Send(buffer, offset, count);
				}
			}
		}

		public void StartReceiving()
		{
			new Thread(() =>
			{
				try
				{
					Uri uri = new Uri(SystemSettings.Host + "Command/Receive?ID=" + Handle + "&tick=" + SystemSettings.GetTick());
					HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(uri);
					HttpWebResponse responce = (HttpWebResponse)webrequest.GetResponse();
					Stream stream = responce.GetResponseStream();
					if (Event != null) Event("Start receiving: " + this);

					for (int read = stream.Read(buffer, 0, buffer.Length); read != 0; read = stream.Read(buffer, 0, buffer.Length))
						onDataReceived(buffer, read);
				}
				catch (Exception exc)
				{
					if (Error != null) Error("Receive error: " + this, exc);
				}
				finally
				{
					Close();
				}
			}).Start();
		}

		protected void onDataReceived(byte[] data, int count)
		{
			if (Event != null) Event("Received " + this);
			if (DataReceived != null)
				DataReceived(this, data, count);
		} 
		#endregion

		public void ModifyDestination(ConnectionInfo conInfo)
		{
			if (reconnect != null)
				conInfo.Parse(reconnect);
		}

		public override string ToString()
		{
			string id = Handle;
			if (id == null)
				id = ticket;

			return "Http " + id.Substring(0, 3) + ' ' + isSource;
		}
	}
}
