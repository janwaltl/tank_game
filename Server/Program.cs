using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace Server
{
	class Program
	{
		class ServerState
		{
			public Socket listener;
			public int numClients;
		}
		/// <summary>
		/// Starts listening for incoming connections to the server.
		/// </summary>
		static async Task StarListening()
		{
			//TODO Error checking for this method

			var endPoint = new IPEndPoint(IPAddress.Any, 23545);
			ServerState serverState = new ServerState
			{
				listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
				numClients = 0
			};
			serverState.listener.Bind(endPoint);
			serverState.listener.Listen(5);

			while (true)
			{
				Console.WriteLine("Listening for a client...");
				Socket client = await Task.Factory.FromAsync(serverState.listener.BeginAccept, serverState.listener.EndAccept, null);
				//RESOLVe
				_ = HandleClientAsync(client, serverState.numClients++);
			}
		}
		/// <summary>
		/// Handles newly connected client. 
		/// </summary>
		/// <param name="client">Incoming socket</param>
		/// <param name="ID">ID of the client</param>
		/// <returns>Task that completes when connections ends</returns>
		static async Task HandleClientAsync(Socket client, int ID)
		{
			NetworkStream networkStream = new NetworkStream(client);
			networkStream.ReadTimeout = 5000;
			byte[] buffer = new byte[1024];
			Console.WriteLine($"Connected to {ID}");
			try
			{
				while (client.Connected)
				{
					int recieved = await networkStream.ReadAsync(buffer, 0, buffer.Length);
					byte[] bMsg = new byte[recieved];
					Array.Copy(buffer, bMsg, recieved);
					string msg = Encoding.ASCII.GetString(bMsg);
					if (msg == "quit")
					{
						client.Shutdown(SocketShutdown.Both);
						client.Close();
					}
					Console.WriteLine($"{ID}:{msg}");
				}
				Console.WriteLine($"Disconnected from {ID}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Forced disconnection from {ID} because:");
				Console.WriteLine(e.Message);
			}
			//TODO other exceptions?
		}
		static void Main(string[] args)
		{
			//RESOLVE
			_ = StarListening();
			System.Threading.Thread.Sleep(60000);
		}
	}

}

