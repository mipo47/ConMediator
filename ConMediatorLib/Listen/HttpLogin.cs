using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace ConMediator
{
	public class HttpLogin
	{
		protected string ticket;

		public void Login()
		{
			Login(SystemSettings.UserName, SystemSettings.Password);
		}

		public void Login(string username, string password)
		{
			string result = SendReceive(SystemSettings.Host + 
				"Account/Login?UserName=" + username + 
				"&Password=" + password + 
				"&RememberMe=false" +
				"&tick=" + SystemSettings.GetTick());
			
			if (!result.StartsWith("Ok"))
				throw new Exception("Login failed");
			else
				ticket = result.Substring(3);
		}

		public string SendReceive(string uri, string text = null)
		{
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
			if (text != null && text != string.Empty)
			{
				byte[] data = Encoding.ASCII.GetBytes(text);

				request.Method = "POST";
				request.ContentLength = data.Length;
				request.ContentType = "application/x-www-form-urlencoded";

				Stream stream = request.GetRequestStream();
				stream.Write(data, 0, data.Length);
				stream.Close();
			}

			Trace.WriteLine("Request to " + uri);
			WebResponse response = request.GetResponse();
			StreamReader reader = new StreamReader(response.GetResponseStream());
			return reader.ReadToEnd();
		}
	}
}
