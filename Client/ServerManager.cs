﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;


namespace Client
{
	/// <summary>
	///Abstracts communictaion with the server
	/// </summary>
	class ServerManager : IDisposable
	{
		/// <summary>
		/// Connects to the server and asynchronously begins receiving the static data.
		/// </summary>
		/// <param name="serverConAddress"></param>
		public ServerManager(IPEndPoint serverConAddress)
		{
			//Same IP adress, different port
			serverUpdateAddress = new IPEndPoint(serverConAddress.Address, Ports.clientUpdates);
			active = true;
			serverConnection = new Socket(serverConAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			//TODO Catch failed connection
			serverConnection.Connect(serverConAddress);

			StaticData = ReceiveStaticDataAsync();
			serverCommands = ImmutableList<ServerCommand>.Empty;
		}
		/// <summary>
		/// Is set and started in ctor
		/// </summary>
		public Task<ConnectingStaticData> StaticData { get; }
		//Finishes the connecting phase by starting to (async) listen to ServerCommands and received DynamicData.
		//Can only be called after StaticData is received.
		public Task<ConnectingDynamicData> FinishConnecting()
		{
			if (StaticData == null || !StaticData.IsCompleted)
				throw new InvalidOperationException("Cannot finishing connecting before static data is received.");
			//TEMP Shift ports by playerID=Allows multiple clients on one computer
			int pID = StaticData.Result.PlayerID;
			var listenOn = new IPEndPoint(IPAddress.Loopback, Ports.serverUpdates + pID);
			ListenForServerCommandsAsync(listenOn).Detach();

			updatesToServer = new Socket(serverUpdateAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			return ReceiveDynamicDataAsync();
		}
		/// <summary>
		/// Returns received commands from the server.
		/// </summary>
		public IEnumerable<ServerCommand> PollServerCommands()
		{
			return Interlocked.Exchange(ref serverCommands, ImmutableList<ServerCommand>.Empty);
		}
		/// <summary>
		/// Sends the client update to the server.
		/// </summary>
		/// <returns>Task representing sent update.</returns>
		public async Task SendClientUpdateAsync(ClientUpdate update)
		{
			await Communication.UDPSendMessageAsync(updatesToServer, serverUpdateAddress, ClientUpdate.Encode(update));
		}

		async Task<ConnectingStaticData> ReceiveStaticDataAsync()
		{
			var sData = ConnectingStaticData.Decode(await Communication.TCPReceiveMessageAsync(serverConnection));
			Console.WriteLine("Received static data from the server.");
			return sData;
		}
		//Begins to receive dynamic data from the server.
		async Task<ConnectingDynamicData> ReceiveDynamicDataAsync()
		{
			if (StaticData == null)
				throw new InvalidOperationException("Cannot start receiving dynamic data before receiving the static data.");
			await StaticData;

			//Confirm that static part received and started listening for commands.
			await Communication.TCPSendACKAsync(serverConnection);
			Console.WriteLine("Sent server ACK.");
			var dynData = ConnectingDynamicData.Decode(await Communication.TCPReceiveMessageAsync(serverConnection));
			Console.WriteLine("Received dynamic data.");
			serverConnection.Shutdown(SocketShutdown.Send);
			Console.WriteLine("Successfully connected to the server.");
			serverConnection.Close();
			serverConnection = null;
			return dynData;
		}

		/// <summary>
		/// Starts listesting for server updates, any received updates are pushed into serverCommands queue.
		/// </summary>
		/// <param name="listenOn">local IP+port on which should the client listen for the updates.</param>
		/// <returns>when server ends the connection or <paramref name="cancelUpdatesFromServer"/>'' is set.</returns>
		async Task ListenForServerCommandsAsync(IPEndPoint listenOn)
		{
			updatesFromServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			updatesFromServer.Bind(listenOn);
			Console.WriteLine($"Started listening for server updates on {listenOn}");
			while (active)
			{
				var bytes = await Communication.UDPReceiveMessageAsync(updatesFromServer, 1024);
				var msg = ServerCommand.Decode(bytes.Item1);

				var tmp = serverCommands;
				while (tmp != Interlocked.CompareExchange(ref serverCommands, tmp.Add(msg), tmp))
					tmp = serverCommands;
			}
			updatesFromServer.Shutdown(SocketShutdown.Both);
			updatesFromServer.Close();
		}

		public void Dispose()
		{
			active = false;
			((IDisposable)serverConnection)?.Dispose();
			((IDisposable)StaticData)?.Dispose();
			((IDisposable)updatesToServer)?.Dispose();
			//Should also break the await ReceiveMessage
			((IDisposable)updatesFromServer)?.Dispose();
		}

		//Whether the communication channels are active
		bool active;
		IPEndPoint serverUpdateAddress;
		Socket updatesToServer;
		Socket updatesFromServer;
		Socket serverConnection;
		ImmutableList<ServerCommand> serverCommands;
	}
}
