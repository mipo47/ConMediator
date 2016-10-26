using System;
using System.Collections.Generic;

using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using ConMediator.Connector;

namespace ConMediator.Listen
{
	class HttpListen : Listen
	{
		HttpConnection connection;
		string listenPort;
		Thread thread;
		bool isStarted = false;

		public override void Start(string listenPort)
		{
			this.listenPort = listenPort;
			thread = new Thread(WaitForConnection);
			isStarted = true;
			thread.Start();
		}

		public override void Stop()
		{
			if (connection != null)
				connection.StopWaiting = true;

			isStarted = false;
		}

		public override event NewConnection Connected;

		void WaitForConnection()
		{
			while (isStarted)
			{
				try
				{
					connection = new HttpConnection(true);
					connection.Connect(listenPort);

					if (!connection.StopWaiting && connection.Connected && Connected != null)
					{
						new Thread(conObj => 
						{
							Connected(conObj as IConnection); 
						}).Start(connection);
					}
				}
				catch (Exception exc)
				{
					Trace.Write(exc.Message);
				}
			}
		}
	}
}
