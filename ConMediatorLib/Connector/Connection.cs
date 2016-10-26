using System;
using System.Collections.Generic;
using System.Text;

namespace ConMediator.Connector
{	
	public abstract class Connection : IConnection
	{
		public const int BUFFER_SIZE = 8192;

		public static IConnection CreateConnection(ConnectionType conType)
		{
			switch (conType)
			{
				case ConnectionType.Serial:
					return new SerialConnection();
				case ConnectionType.TCP:
					return new TcpConnection();
				case ConnectionType.HTTP:
					return new HttpConnection(false);
			}
			return null;
		}

		public event DataReceivedDelegate DataReceived;
		public event NewEvent Event;
		public event ErrorException Error;

		protected void onDataReceived(byte[] data, int count)
		{
			if (DataReceived != null)
				DataReceived(this, data, count);
		}

		#region Abstract methods, properties

		public abstract string Handle { get; }
		public abstract bool Connected { get; }
		public abstract ConnectionType ConnectionType { get; }

		public abstract void Connect(string connectionString);
		public abstract void Close();

		public abstract void StartReceiving();
		public abstract void Send(byte[] buffer, int offset, int count);

		#endregion
	}
}
