using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace Server
{
	class Program
	{
		static void Main(string[] args)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32653));

			socket.Listen(1);
			var s = socket.Accept();
			byte[] buff = new byte[s.ReceiveBufferSize];
			while (true)
			{
				int size = s.Receive(buff);
				byte[] msg = new byte[size];
				Array.Copy(buff, msg, size);
				Console.WriteLine(Encoding.BigEndianUnicode.GetString(msg));
			}
		}

	}
}
