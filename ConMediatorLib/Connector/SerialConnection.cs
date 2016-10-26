using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace ConMediator.Connector
{
	class SerialConnection : Connection
	{
		static Dictionary<string, SerialPort> connectedSerials = new Dictionary<string, SerialPort>();

		byte[] buffer = new byte[BUFFER_SIZE];
		SerialPort connection;

		public override ConnectionType ConnectionType { get { return ConnectionType.Serial; } }

		public override bool Connected
		{
			get { return connection == null ? false : connection.IsOpen; }
		}

		public override string Handle
		{
			get { return connection.PortName; }
		}

		public override void Connect(string connectionString)
		{
			string[] parts = connectionString.Split(':');

			if (!connectedSerials.ContainsKey(connectionString))
			{
				connection = new SerialPort();

				connection.PortName = parts[0];
				connection.BaudRate = int.Parse(parts[1]);

				switch (parts[2][0])
				{
					case 'N':
						connection.Parity = Parity.None;
						break;
					case 'E':
						connection.Parity = Parity.Even;
						break;
					case 'M':
						connection.Parity = Parity.Mark;
						break;
					case 'O':
						connection.Parity = Parity.Odd;
						break;
					case 'S':
						connection.Parity = Parity.Space;
						break;

					default:
						throw new Exception("Parity not suported: " + parts[2][0]);
				}

				// data bits
				switch (parts[2][2])
				{
					case '5':
						connection.DataBits = 5;
						break;
					case '6':
						connection.DataBits = 6;
						break;
					case '7':
						connection.DataBits = 7;
						break;
					case '8':
						connection.DataBits = 8;
						break;

					default:
						throw new Exception("Data bits not suported: " + parts[2][2]);
				}

				// stop bits
				switch (parts[2][4])
				{
					case '1':
						if (parts[2].Length > 5)
							connection.StopBits = StopBits.OnePointFive;
						else
							connection.StopBits = StopBits.One;
						break;

					case '2':
						connection.StopBits = StopBits.Two;
						break;

					case 'N':
						connection.StopBits = StopBits.None;
						break;

					default:
						throw new Exception("Stop bits not suported: " + parts[2][4]);
				}

				connection.Open();
				connection.Close();
				connection.Open();
				connectedSerials.Add(connectionString, connection);
			}
			else
			{
				connection = connectedSerials[connectionString];
				if (!connection.IsOpen)
					connection.Open();
			}

			connection.DataReceived += connection_DataReceived;
		}

		public override void Close()
		{
			connection.DataReceived -= connection_DataReceived;
			connection.Close();
		}

		public override void StartReceiving() { }

		public override void Send(byte[] buffer, int offset, int count)
		{
			connection.Write(buffer, offset, count);
		}

		void connection_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			byte[] buffer = this.buffer;
			int count = connection.BytesToRead;
			if (count > buffer.Length)
				buffer = new byte[count];

			connection.Read(buffer, 0, count);
			onDataReceived(buffer, count);
		}
	}
}
