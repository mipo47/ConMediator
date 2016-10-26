using System;
using System.Collections.Generic;

using System.Text;

namespace ConMediator.Listen
{
	public abstract class Listen : IListen
	{
		public static IListen CreateListen(ListenType lisType)
		{
			switch (lisType)
			{
				case ListenType.TCP:
					return new TcpListen();

				case ListenType.HTTP:
					return new HttpListen();
			}
			return null;
		}

		public abstract void Start(string listen);
		public abstract void Stop();
		public abstract event NewConnection Connected;
	}
}
