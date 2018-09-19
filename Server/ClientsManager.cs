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
using Engine;
using OpenTK;

using System.Collections.Immutable;

namespace Server
{
	/// <summary>
	/// Abstracts communication with the clients.
	/// Listens for their connections, update and is able to send them ServerCommands.
	/// </summary>
	class ClientsManager : IDisposable
	{
		/// <summary>
		/// Successfully connected, playing client.
		/// </summary>
		public class ConnectedClient
		{
			public int playerID;
			/// <summary>
			/// Where to send updated game state
			/// </summary>
			public IPEndPoint updateAddress;
			/// <summary>
			/// Channel where to send reliable server updates.
			/// </summary>
			public Socket relUpdateSocket;
			/// <summary>
			/// Number of server ticks without update from the client that will result in disconnection.
			/// </summary>
			public int timeoutTicks;
			public int lastPolledCUpdate;
		}
		/// <summary>
		/// Generates StaticData for newly connected clients.
		/// </summary>
		/// <param name="assignedPlayerID">Player ID of the new client.</param>
		/// <returns></returns>
		public delegate ConnectingStaticData SDataGeneratorDelegate(int assignedPlayerID);
		/// <summary>
		/// Starts listening for connections and updates.
		/// </summary>
		/// <param name="sDataGenerator">Function that generates static data for newly connected clients.</param>
		public ClientsManager(SDataGeneratorDelegate sDataGenerator)
		{
			active = true;
			readyClients = ImmutableList<ReadyClient>.Empty;
			connectedClients = new Dictionary<int, ConnectedClient>();
			clientUpdates = ImmutableList<ClientUpdate>.Empty;
			serverCommandsSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			ListenForClientUpdatesAsync().Detach();
			ListenForConnectionsAsync(sDataGenerator).Detach();
		}
		/// <summary>
		/// Returns newly received ClientUpdates from connected clients.
		/// Resets timeoutTicks for processed updates and updates clients' lastPolledCUpdates
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ClientUpdate> PollClientUpdates()
		{
			foreach (var update in Interlocked.Exchange(ref clientUpdates, ImmutableList<ClientUpdate>.Empty))
			{
				if (connectedClients.TryGetValue(update.PlayerID, out ConnectedClient client))
				{
					if (client.lastPolledCUpdate < update.ID)
						client.lastPolledCUpdate = update.ID;
					//Reset time-out counter
					client.timeoutTicks = 0;
					yield return update;
				}
			}
		}
		/// <summary>
		/// Goes through ready players, async send them dynamic data and then returns their IDs.
		/// Ready client means that they received static data and requested sending dynamicData.
		/// </summary>
		/// <returns>Commands representing newly connected players.</returns>
		public IEnumerable<int> ProcessReadyClients(ConnectingDynamicData dynamicData)
		{
			//Claim the queue
			var queue = readyClients;
			readyClients = ImmutableList<ReadyClient>.Empty;

			var dynDataBytes = ConnectingDynamicData.Encode(dynamicData);
			foreach (var c in queue)
				ProcessReadyClient(c, dynDataBytes).Detach();
			return from c in queue select c.playerID;
		}
		/// <summary>
		/// Tick connected players, returns IDs of timed-out players.
		/// </summary>
		public IEnumerable<int> TickClients(int ticksToTimeout)
		{
			//Remove nonresponding clients
			var toBeDeleted = new List<int>();
			foreach (var c in connectedClients.Values)
				if (++c.timeoutTicks >= ticksToTimeout)
				{
					Console.WriteLine($"{c.playerID} has been timed out.");
					toBeDeleted.Add(c.playerID);
				}
			foreach (var pID in toBeDeleted)
				connectedClients.Remove(pID);
			return toBeDeleted;
		}
		/// <summary>
		/// Sends a ServerCommand to a client from connectClients.
		/// </summary>
		/// <param name="pID">ID of the client.</param>
		/// <param name="cmd">Command to send.</param>
		public async Task SendServerCommandAsync(int pID, ServerCommand cmd)
		{
			if (connectedClients.TryGetValue(pID, out ConnectedClient client))
			{
				if (cmd.guaranteedExec)
				{
					var bytes = Serialization.PrependInt(new CmdServerUpdate(cmd).Encode(), client.lastPolledCUpdate);
					await Task.Delay(250);
					await Communication.TCPSendMessageAsync(client.relUpdateSocket, bytes);
				}
				else
				{
					var bytes = Serialization.PrependInt(cmd.Encode(), client.lastPolledCUpdate);
					await Task.Delay(250);
					await Communication.UDPSendMessageAsync(serverCommandsSender, client.updateAddress, bytes);
				}
			}
			//RESOLVE else throw?
		}
		/// <summary>
		/// Sends a command to all currently connected clients.
		/// </summary>
		/// <param name="cmdID">TODO</param>
		/// <param name="cmd">Command to send.</param>
		public void BroadCastServerCommand(int cmdID, ServerCommand cmd)
		{
			foreach (var pID in GetConnectedClientsIDs())
				SendServerCommandAsync(pID, cmd).Detach();
		}
		public IEnumerable<int> GetConnectedClientsIDs()
		{
			return connectedClients.Keys;
		}
		/// <summary>
		/// Client that received static data, started listening for updates(=sent ACK) and waits for dynamic data.
		/// </summary>
		class ReadyClient
		{
			public int playerID;
			/// <summary>
			/// Connected socket to which server can send dynamic data.
			/// </summary>
			public Socket dynDataSocket;
			/// <summary>
			/// Connected socket where clients waits for reliable updates.
			/// </summary>
			public Socket relUpdatesSocket;
		}
		/// <summary>
		/// Starts listening for incoming connections to the server.
		/// </summary>
		async Task ListenForConnectionsAsync(SDataGeneratorDelegate sDataGenerator)
		{
			//TODO Error checking for this method

			var endPoint = new IPEndPoint(IPAddress.Any, Ports.serverConnection);
			connectionListener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			/// Next available client/player ID.
			int nextClientID = 0;

			connectionListener.Bind(endPoint);
			connectionListener.Listen(5);
			while (active)
			{
				Console.WriteLine("Listening for a client...");
				Socket client = await Task.Factory.FromAsync(connectionListener.BeginAccept, connectionListener.EndAccept, null);
				int playerID = nextClientID++;
				ConnectingStaticData con = sDataGenerator(nextClientID++);
				HandleClientConnectionAssync(client, playerID, con).Detach();
			}
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


			//RESOLVE when client is not ready
			bool clientReady = await Communication.TCPReceiveACKAsync(client);
			Console.WriteLine($"Recieved ACK from {ID}");
			Debug.Assert(clientReady);
			ReadyClient c = new ReadyClient
			{
				playerID = con.PlayerID,
				dynDataSocket = client,
				relUpdatesSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
			};
			//Establish a channel for reliable updates.
			//TEMP Shift ports by playerID=Allows multiple clients on one computer
			var relAddress = new IPEndPoint((c.dynDataSocket.RemoteEndPoint as IPEndPoint).Address, Ports.relServerUpdates + con.PlayerID);
			c.relUpdatesSocket.Connect(relAddress);
			Console.WriteLine($"Created a reliable channel for server commands fro {ID}.");
			var tmp = readyClients;
			while (tmp != Interlocked.CompareExchange(ref readyClients, tmp.Add(c), tmp))
				tmp = readyClients;

			Console.WriteLine($"{ID} is ready");
		}

		/// <summary>
		/// Adds client to connectedClients then asynchronously sends the dynamic data to them and disconnects the socket. 
		/// </summary>
		/// <returns>Task that finishes when the message has been sent.</returns>
		async Task ProcessReadyClient(ReadyClient c, byte[] dynamicData)
		{
			var cc = new ConnectedClient
			{
				playerID = c.playerID,
				timeoutTicks = 0,
				relUpdateSocket = c.relUpdatesSocket,
				lastPolledCUpdate = 0,
			};
			Debug.Assert(c.dynDataSocket.RemoteEndPoint is IPEndPoint, "Socket should use IP for communication,");
			//Use address from previous connection.
			//TEMP Shift ports by playerID=Allows multiple clients on one computer
			cc.updateAddress = new IPEndPoint((c.dynDataSocket.RemoteEndPoint as IPEndPoint).Address, Ports.serverUpdates + c.playerID);
			connectedClients.Add(cc.playerID, cc);
			Console.WriteLine($"Client {cc.playerID} is now connected.");

			await Communication.TCPSendMessageAsync(c.dynDataSocket, dynamicData);
			c.dynDataSocket.Shutdown(SocketShutdown.Send);

			int read = c.dynDataSocket.Receive(new byte[5]);//Wait until clients shutdowns its socket = dynamic data received.
			Debug.Assert(read == 0);
			Console.WriteLine($"Dynamic data for {cc.playerID} has been sent,closing connection.");
			c.dynDataSocket.Close();
		}

		/// <summary>
		/// Listen for UDP client updates,
		/// </summary>
		/// <returns></returns>
		async Task ListenForClientUpdatesAsync()
		{
			var endPoint = new IPEndPoint(IPAddress.Any, Ports.clientUpdates);
			cUpdateListener = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			cUpdateListener.Bind(endPoint);
			Console.WriteLine($"Started listening for updates on {endPoint}");
			while (active)
			{
				//RESOLVE ensure that all updates can fit into this.
				var msg = await Communication.UDPReceiveMessageAsync(cUpdateListener, 1024);

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
			var tmp = clientUpdates;
			while (tmp != Interlocked.CompareExchange(ref clientUpdates, tmp.Add(clientUpdate), tmp))
				tmp = clientUpdates;
		}
		/// <summary>
		/// Ends all communication with the clients.
		/// </summary>
		public void Dispose()
		{
			active = false;
			serverCommandsSender.Dispose();
			cUpdateListener.Dispose();
			connectionListener.Dispose();
			foreach (var c in readyClients)
			{
				c.dynDataSocket.Dispose();
				c.relUpdatesSocket.Dispose();
			}
		}

		//Whether the communication channels are active
		bool active;

		ImmutableList<ReadyClient> readyClients;
		ImmutableList<ClientUpdate> clientUpdates;
		//Key is the PlayerID
		Dictionary<int, ConnectedClient> connectedClients;
		Socket serverCommandsSender;
		Socket cUpdateListener;
		Socket connectionListener;
	}
}
