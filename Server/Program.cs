﻿using System;
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
	/// <summary>
	/// Successfully connected, playing client.
	/// </summary>
	class ConnectedClient
	{
		public int playerID;
		//Where to send updated game state
		public IPEndPoint updateAddress;
		//Number of server ticks without update from the client that will result in disconnection.
		public int timeoutTicks;
	}
	/// <summary>
	/// Client that received static data, started listening for updates(=sent ACK) and waits for dynamic data.
	/// </summary>
	struct ReadyClient
	{
		public int playerID;
		public Socket socket;
	}

	class Program
	{
		/// <summary>
		/// Creates server
		/// </summary>
		/// <param name="tickTime">How much one tick takes in milliseconds</param>
		/// <param name="timeoutTime">How much time should pass from last client's update before the player is timed out.In seconds</param>
		public Program(double tickTime, double timeoutTime)
		{
			this.tickTime = tickTime;
			ticksToTimeout = (int)(timeoutTime / tickTime * 1000.0);

			clientUpdates = new ConcurrentQueue<ClientUpdate>();
			connectedClients = new Dictionary<int, ConnectedClient>();
			readyClients = new ConcurrentQueue<ReadyClient>();
		}
		/// <summary>
		/// Starts listening for incoming connections to the server.
		/// </summary>
		async Task ListenForConnectionsAsync()
		{
			//TODO Error checking for this method

			var endPoint = new IPEndPoint(IPAddress.Any, Ports.serverConnection);

			conListener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			nextClientID = 0;


			conListener.Bind(endPoint);
			conListener.Listen(5);

			while (true)
			{
				Console.WriteLine("Listening for a client...");
				Socket client = await Task.Factory.FromAsync(conListener.BeginAccept, conListener.EndAccept, null);
				int playerID = nextClientID++;
				ConnectingStaticData con = new ConnectingStaticData(playerID, $"Testing message for client {playerID}");
				HandleClientConnectionAssync(client, playerID, con).Detach();
			}
		}
		/// <summary>
		/// Listen for UDP client updates,
		/// </summary>
		/// <returns></returns>
		async Task ListenForClientUpdatesAsync()
		{
			var endPoint = new IPEndPoint(IPAddress.Any, Ports.clientUpdates);
			updListener = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			updListener.Bind(endPoint);
			Console.WriteLine($"Started listening for updates on {endPoint}");
			while (true)
			{
				//RESOLVE ensure that all updates can fit into this.
				var msg = await Communication.UDPReceiveMessageAsync(updListener, 1024);

				HandleClientUpdateAsync(msg.Item1).Detach();
			}
		}
		/// <summary>
		/// Processes clientUpdate and adds it to the queue.
		/// </summary>
		/// <param name="msg">ClientUpdate</param>
		/// <returns>Task that completes when the ClienUpdate has been enqueued.</returns>
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
		async Task HandleClientConnectionAssync(Socket client, int ID, ConnectingStaticData con)
		{
			Console.WriteLine($"Connectd to {ID}({client.RemoteEndPoint})");
			await Communication.TCPSendMessageAsync(client, ConnectingStaticData.Encode(con));

			ReadyClient c = new ReadyClient
			{
				socket = client,
				playerID = con.playerID
			};

			bool clientReady = await Communication.TCPReceiveACKAsync(client);
			//RESOLVE when client is not ready
			readyClients.Enqueue(c);
			Console.WriteLine($"{ID} is ready");
		}


		/// <summary>
		/// Infinite loop that implementes server logic
		/// </summary>
		void RunUpdateLoop()
		{
			updBroadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			Stopwatch watch = Stopwatch.StartNew();
			double accumulator = 0;
			while (true)
			{
				ProcessReadyClients();
				ProcessCommandQueue();
				//TODO Run game logic

				BroadcastUpdates();

				accumulator = TickTiming(tickTime, watch, accumulator);
				watch.Restart();
			}
		}
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
		/// <summary>
		/// Empties current command queue and processes it. Resets timeout counter for players that
		/// </summary>
		void ProcessCommandQueue()
		{
			var queue = Interlocked.Exchange(ref clientUpdates, new ConcurrentQueue<ClientUpdate>());
			//CURRENTLY just prints the updates
			if (queue.Count > 0)
				Console.WriteLine($"({queue.Count})updates:");
			foreach (var item in queue)
			{
				//Reset timeout ticks
				//TODO Ignore wrong playerIDs? - timed-out players
				connectedClients[item.playerID].timeoutTicks = 0;

				//TODO Proccess the item
				//Console.WriteLine(item.msg);
			}
		}
		/// <summary>
		/// Goes through ready players and sends them dynamic data = other players, missiles.
		/// Then adds them to the list of connected players.
		/// </summary>
		void ProcessReadyClients()
		{
			//Prepare dynamic data
			var dynamicData = ConnectingDynamicData.Decode(new ConnectingDynamicData("Test of dynamic data."));
			//Claim the queue
			var queue = readyClients;
			readyClients = new ConcurrentQueue<ReadyClient>();

			foreach (var c in queue)
				ProcessReadyClient(c, dynamicData).Detach();
		}
		/// <summary>
		/// Adds client to connectedClients then asynchronously sends the dynamic data to them and disconnects the socket. 
		/// </summary>
		/// <returns>Task that finishes when the message has been sent.</returns>
		private async Task ProcessReadyClient(ReadyClient c, byte[] dynamicData)
		{
			var cc = new ConnectedClient
			{
				playerID = c.playerID,
				timeoutTicks = 0
			};
			Debug.Assert(c.socket.RemoteEndPoint is IPEndPoint, "Socket should use IP for communication,");
			//Use address from previous connection.
			cc.updateAddress = new IPEndPoint((c.socket.RemoteEndPoint as IPEndPoint).Address, Ports.serverUpdates);
			connectedClients.Add(cc.playerID, cc);
			Console.WriteLine($"Client {cc.playerID} is now connected.");

			await Communication.TCPSendMessageAsync(c.socket, dynamicData);
			c.socket.Shutdown(SocketShutdown.Send);

			int read = c.socket.Receive(new byte[5]);//Wait until clients shutdowns its socket = dynamic data received.
			Debug.Assert(read == 0);
			Console.WriteLine($"Dynamic data for {cc.playerID} has been sent,closing connection.");
			c.socket.Close();
		}
		/// <summary>
		/// Sends new server state to all connected clients.
		/// </summary>
		private void BroadcastUpdates()
		{
			//Remove nonresponding clients
			var toBeDeleted = new List<int>();
			foreach (var c in connectedClients.Values)
				if (++c.timeoutTicks >= ticksToTimeout)
				{
					Console.WriteLine($"{c.playerID} has been timed out.");
					toBeDeleted.Add(c.playerID);
				}

			//TODO prepare the update, include info about disconnects
			byte[] update = ServerCommand.Encode(new ServerCommand("Broadcast msg from the server."));

			foreach (var pID in toBeDeleted)
				connectedClients.Remove(pID);

			//Send updates, do not wait for them for now
			//IMPROVE do not fall to much behind with these broadcasts
			foreach (var c in connectedClients.Values)
				Communication.UDPSendMessageAsync(updBroadcast, c.updateAddress, update).Detach();
		}
		private Socket conListener;
		private Socket updListener;
		private Socket updBroadcast;
		/// <summary>
		/// Received ClientUpdates that will be processed in the next tick.
		/// </summary>
		private ConcurrentQueue<ClientUpdate> clientUpdates;
		/// <summary>
		/// Connected clients, key is clientID, 
		/// </summary>
		private Dictionary<int, ConnectedClient> connectedClients;
		/// <summary>
		/// Clients that have downloaded static data=map and are ready to downlaod dynamic data and begin to recieve updates
		/// </summary>
		private ConcurrentQueue<ReadyClient> readyClients;
		/// <summary>
		/// Next available client/player ID.
		/// </summary>
		private int nextClientID;
		/// <summary>
		/// Client will be timed-out if they won't sent any ClientUpdates for this amount of server ticks.
		/// </summary>
		private readonly int ticksToTimeout;
		/// <summary>
		/// Amount of time between two server ticks in miliseconds.
		/// </summary>
		private readonly double tickTime;

		static void Main(string[] args)
		{
			Program server = new Program(200.0, 10.0);
			server.ListenForConnectionsAsync().Detach();
			server.ListenForClientUpdatesAsync().Detach();
			server.RunUpdateLoop();
		}
	}

}

