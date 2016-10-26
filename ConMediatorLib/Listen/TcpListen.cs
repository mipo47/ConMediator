using System;
using System.Collections.Generic;

using System.Text;
using System.Net.Sockets;
using System.Net;
using ConMediator.Connector;
using System.Diagnostics;

namespace ConMediator.Listen
{
	class TcpListen : Listen
	{
		TcpListener listener;

		public override event NewConnection Connected;

		public override void Start(string listen)
		{
			int port = int.Parse(listen);
			listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			listener.BeginAcceptSocket(OnNewConnection, null);
		}

		public override void Stop()
		{
			listener.Stop();
		}

		void OnNewConnection(IAsyncResult result)
		{
			Socket socket;
			IConnection connection = null;
			try
			{
				socket = listener.EndAcceptSocket(result);
				connection = new TcpConnection(socket);
			}
			catch (Exception exc)
			{
				Trace.Write(exc.Message);
			}
			finally
			{
				try { listener.BeginAcceptSocket(OnNewConnection, null); }
				catch (Exception e) { Trace.Write(e.Message); }
			}

			if (connection != null && Connected != null)
				Connected(connection);
		}
	}
}
