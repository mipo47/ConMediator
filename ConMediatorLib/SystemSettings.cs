using System;
using System.Collections.Generic;
using System.Text;

namespace ConMediator
{
	public static class SystemSettings
	{
		public static readonly Random Random = new Random((int)DateTime.Now.Ticks);

		public static int GetTick()
		{
			return Random.Next();
		}

		public static string Host = "http://localhost/proxy/";
		public static string UserName = "tester";
		public static string Password = "tester";

		public static int PacketSize = 4096;
		public static int ContentSize = 262144;
		public static int RetryTimeout = 5000;
	}
}
