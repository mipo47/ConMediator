using System;
using System.Net.Sockets;

public class AutoAnswer
{
	string request;
	public string GetAnswer(Socket socket, string input)
	{
		request += input;
		if (!request.EndsWith("\r\n"))
			return "";

		switch (request)
		{
			case "Request1\r\n":
				return "Response1\r\n";
			case "Request2\r\n":
				return "Response2\r\n";
		}

		request = "";
		return "Unknown request";
	}
}