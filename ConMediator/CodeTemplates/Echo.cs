using System;
using System.Net.Sockets;

public class AutoAnswer
{
	public byte[] GetRawAnswer(Socket socket, byte[] input)
	{
		return input; 
	}
}