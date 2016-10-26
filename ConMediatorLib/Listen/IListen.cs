using System;
using System.Collections.Generic;

using System.Text;
using ConMediator.Connector;

namespace ConMediator.Listen
{
	public enum ListenType
	{
		TCP, HTTP
	}

	public delegate void NewConnection(IConnection connection);

	public interface IListen
	{
		void Start(string listen);
		void Stop();

		event NewConnection Connected;
	}
}
