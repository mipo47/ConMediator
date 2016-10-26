using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using ConMediator.Properties;

namespace ConMediator
{
	class Common
	{
		public static ILog Log { get; private set; }

		static Common()
		{
			log4net.Config.XmlConfigurator.Configure();
			Common.Log = LogManager.GetLogger("CM" + Settings.Default.ListenType + '_' + Settings.Default.ListenPort);
		} 

		public static byte[] ConvertToHEX(string hexString)
		{
			hexString = hexString.Replace(" ", "").ToUpper();
			byte[] buffer = new byte[hexString.Length / 2];

			for (int i = 0; i < buffer.Length; i++)
				buffer[i] = byte.Parse(hexString.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

			return buffer;
		}
	}
}
