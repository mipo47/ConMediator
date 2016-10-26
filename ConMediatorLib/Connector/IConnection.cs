using System;
using System.Collections.Generic;

using System.Text;

namespace ConMediator.Connector
{
	public enum ConnectionType
	{
		Serial, TCP, HTTP
	}

	public delegate void DataReceivedDelegate(IConnection connection, byte[] data, int count);

	public interface IConnection
	{
		event DataReceivedDelegate DataReceived;
		event NewEvent Event;
		event ErrorException Error;

		//string ConnectionString { get; }
		string Handle { get; }
		bool Connected { get; }
		ConnectionType ConnectionType { get; }

		void Connect(string connectionString);
		void Close();

		void StartReceiving();
		void Send(byte[] buffer, int offset, int count);
	}
}
