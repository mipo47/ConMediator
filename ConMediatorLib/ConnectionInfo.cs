using System;
using System.Collections.Generic;

using System.Text;
using ConMediator.Connector;

namespace ConMediator
{
	class ConnectionInfo
	{
		public ConnectionType ConnectionType;
		public string ConnectionString;

		public void Parse(string connection)
		{
			int index = connection.IndexOf(' ');
			string type = connection.Substring(index).ToUpper();
			ConnectionString = connection.Substring(index + 1);
			switch (type)
			{
				case "SERIAL": ConnectionType = Connector.ConnectionType.Serial; break;
				case "TCP": ConnectionType = Connector.ConnectionType.TCP; break;
				case "HTTP": ConnectionType = Connector.ConnectionType.HTTP; break;
			}
		}

		public override string ToString()
		{
			return ConnectionType + ' ' + ConnectionString;
		}
	}
}
