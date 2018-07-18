using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;

namespace Server
{
	class Program
	{
		public Program(int connectionPort, int updatePort)
		{
			conPort = connectionPort;
			updPort = updatePort;
			clientUpdates = new ConcurrentQueue<ClientUpdate>();
		}
		/// <summary>
		/// Starts listening for incoming connections to the server.
		/// </summary>
		async Task ListenForConnectionsAsync()
		{
			//TODO Error checking for this method

			var endPoint = new IPEndPoint(IPAddress.Any, conPort);

			conListener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			nextClientID = 0;


			conListener.Bind(endPoint);
			conListener.Listen(5);

			while (true)
			{
				Console.WriteLine("Listening for a client...");
				Socket client = await Task.Factory.FromAsync(conListener.BeginAccept, conListener.EndAccept, null);
				int playerID = nextClientID++;
				ClientConnecting con = new ClientConnecting(playerID, $"Testing message for client {playerID}");
				HandleClientConnectionAssync(client, playerID, con).Detach();
			}
		}
		async Task ListenForClientUpdatesAsync()
		{
			var endPoint = new IPEndPoint(IPAddress.Any, updPort);
			updListener = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			updListener.Bind(endPoint);
			//TODO ensure that all datagrams can fit into this.
			byte[] buffer = new byte[1024];
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => updListener.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, state);
			while (true)
			{
				var numRead = await Task.Factory.FromAsync(begin, updListener.EndReceive, null);
				byte[] msg = new byte[numRead];
				Array.Copy(buffer, msg, numRead);
				HandleClientUpdateAsync(msg).Detach();
			}
		}
		async Task HandleClientUpdateAsync(byte[] msg)
		{
			var clientUpdate = await Task.Run(() => ClientUpdate.Decode(msg));

			clientUpdates.Enqueue(clientUpdate);
		}
		/// <summary>
		/// Handles newly connected client. 
		/// </summary>
		/// <param name="client">Incoming socket</param>
		/// <param name="ID">ID of the client</param>
		/// <returns>Task that completes when the client has been handled.</returns>
		async Task HandleClientConnectionAssync(Socket client, int ID, ClientConnecting con)
		{
			Console.WriteLine($"Connectd to {ID}");
			await Communication.TCPSendMessageAsync(client, ClientConnecting.Encode(con));

			client.Shutdown(SocketShutdown.Both);
			client.Close();
			Console.WriteLine($"Disconnected from {ID}");
		}


		/// <summary>
		/// Infinite loop that implementes server logic
		/// </summary>
		void RunUpdateLoop()
		{
			const double tickTime = 16.0;
			Stopwatch watch = Stopwatch.StartNew();
			double accumulator = 0;
			while (true)
			{
				var queue = Interlocked.Exchange(ref clientUpdates, new ConcurrentQueue<ClientUpdate>());
				ProcessCommandQueue(queue);
				//TODO Send updated state to the clients
				accumulator = TickTiming(tickTime, watch, accumulator);
				watch.Restart();
			}
		}
		void ProcessQueue(ConcurrentQueue<ClientUpdate> queue)
		/// <summary>
		/// Computes proper timing of the server loop. Waits if server is running too fast, accumulates debt if too slow.
		/// </summary>
		/// <param name="tickTime">How long should each server tick take</param>
		/// <param name="watch"></param>
		/// <param name="accumulator">Debt from previous tick</param>
		/// <returns>Time debt carried over to the next tick</returns>
		private static double TickTiming(double tickTime, Stopwatch watch, double accumulator)
		{
			var timeTicks = watch.ElapsedTicks;
			double elapsedMS = timeTicks / 1000.0 / Stopwatch.Frequency;
			//We didn't spend enough time processing
			//=>Use it to pay off the accumulator
			if (elapsedMS < tickTime)
			{
				double excessTime = tickTime - elapsedMS;
				if (accumulator > excessTime)//Can't pay everything
					accumulator -= excessTime;
				else//Sleep for the remainder
					Task.Delay((int)(excessTime - accumulator)).Wait();
			}
			else//We've spent too much time processing, subtract it from next tick
				accumulator += tickTime - elapsedMS;
			return accumulator;
		}

		void ProcessCommandQueue(ConcurrentQueue<ClientUpdate> queue)
		{
			//CURRENTLY just prints the updates
			if (queue.Count > 0)
				Console.Write($"({queue.Count}):");
			foreach (var item in queue)
			{
				Console.WriteLine(item.msg);
			}
		}

		private Socket conListener;
		private Socket updListener;
		private ConcurrentQueue<ClientUpdate> clientUpdates;
		private readonly int conPort, updPort;
		private int nextClientID;

		static void Main(string[] args)
		{
			Program server = new Program(23545, 23546);
			server.ListenForConnectionsAsync().Detach();
			server.ListenForClientUpdatesAsync().Detach();
			server.RunUpdateLoop();
		}
	}
	static class TaskExtensions
	{
		/// <summary>
		/// Do not wait for the task.
		/// </summary>
		/// <param name="t"></param>
		public static void Detach(this Task t)
		{
			//Only forget launched tasks for now.
			Debug.Assert(t.Status != TaskStatus.Created);
		}
	}
}

