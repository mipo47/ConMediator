using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.IO;

namespace SoftwareSecurity
{
	class SelfTest
	{
		string trueHash;

		public bool AssemblyIsValid
		{
			get
			{
				MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
				string filename = AppDomain.CurrentDomain.FriendlyName;
				filename = filename.ToLower().Replace(".vshost.", ".");

				FileStream stream = new FileStream(
					filename,
					FileMode.Open,
					FileAccess.Read,
					FileShare.ReadWrite);
				
				byte[] hash = md5.ComputeHash(stream);
				stream.Close();

				string hashString = Convert.ToBase64String(hash);
				return trueHash == hashString;
			}
		}

		public SelfTest(string hash)
		{
			this.trueHash = hash;
		}
	}
}
