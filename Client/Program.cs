using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Client
{
	class Program
	{
		static void Main(string[] args)
		{
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				s.Connect(IPAddress.Parse("127.0.0.1"), 32653); // 2
				Console.Write("Zadej nejakej text : ");
				while (true)
				{
					string q = Console.ReadLine();                 // 3
					byte[] data = Encoding.BigEndianUnicode.GetBytes(q);    // 3
					s.Send(data);
				}
			}
			catch (Exception e) // 1
			{
				Console.WriteLine("Error: \n" + e.Message);
			}
		}
	}
}
