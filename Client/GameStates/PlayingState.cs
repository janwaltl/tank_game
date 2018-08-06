using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;
namespace Client.GameStates
{
	class PlayingState : IGameState, IDisposable
	{
		/// <summary>
		/// Builds from static data, begins to accept the dynamic data
		/// </summary>
		/// <param name="serverAddress">Where to send clientUpdates</param>
		/// <param name="sData">Static data received from the server.</param>
		/// <param name="server">Socket connected to the server via TCP waiting for ACK - see protocols.</param>
		public PlayingState(IPEndPoint serverAddress, ConnectingStaticData sData, Socket server)
		{
			this.sAddress = serverAddress;
			this.playerID = sData.playerID;
			this.serverDynamic = server;
			serverCommands = new Queue<ServerCommand>();
			//TODO Build engine from static data
		}
		public IGameState UpdateState(double dt)
		{
			//Wait for the dynamic data
			//RESOLVE faulted status
			if (finishConnecting.Status == TaskStatus.RanToCompletion)
			{
				SendClientupdate();
				//TODO process received commands
				//	- empty the queue, process

				//TODO Implement gamelogic

			}
			return this;
		}


		public void OnSwitch()
		{
			updatesToServer = new Socket(sAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			//Start listening for commands
			var listenOn = new IPEndPoint((serverDynamic.LocalEndPoint as IPEndPoint).Address, Ports.serverUpdates);
			ListenForServerCommandsAsync(listenOn).Detach();
			//Finish the connecting process
			finishConnecting = FinishConnecting();
		}

		public void Dispose()
		{
			updatesToServer.Shutdown(SocketShutdown.Both);
			updatesToServer.Close();
			cancelUpdatesFromServer = true;
		}
		private async Task FinishConnecting()
		{
			//Confirm that static part received and started listening for commands.
			await Communication.TCPSendACKAsync(serverDynamic);
			Console.WriteLine("Sent server ACK.");
			var data = ConnectingDynamicData.Encode(await Communication.TCPReceiveMessageAsync(serverDynamic));
			Console.WriteLine("Received dynamic data.");
			//TODO Update the engine with dynamic data

			serverDynamic.Shutdown(SocketShutdown.Send);

			// Next implement server broadcasting message and processing here
			// Comment all the new methods - verify the workflow of the network and add it to protocols
			Console.WriteLine("Successfully connected to the server.");
			serverDynamic.Close();
		}
		/// <summary>
		/// Sends updated client state/inputs to the server.
		/// </summary>
		private void SendClientupdate()
		{
			//TODO refactor into UDPSendMessage
			var buffer = Encoding.BigEndianUnicode.GetBytes($"Status update from {playerID}");
			int numSend = updatesToServer.SendTo(buffer, sAddress);
			Debug.Assert(numSend == buffer.Length);
		}
		/// <summary>
		/// Starts listesting for server updates, any received updates are pushed into serverCommands queue.
		/// </summary>
		/// <param name="listenOn">local IP+port on which should the client listen for the updates.</param>
		/// <returns>when server ends the connection or <paramref name="cancelUpdatesFromServer"/>'' is set.</returns>
		private async Task ListenForServerCommandsAsync(IPEndPoint listenOn)
		{
			updatesFromServer = new Socket(listenOn.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			updatesFromServer.Bind(listenOn);
			Console.WriteLine("Started listening for server updates.");
			byte[] buffer = new byte[1024];//TODO ensure that packets send by server are smaller than this
			EndPoint sender = sAddress;

			Func<AsyncCallback, object, IAsyncResult> begin =
				(callback, state) => updatesFromServer.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
																		ref sender, callback, state);
			Func<IAsyncResult, int> end = (callback) => updatesFromServer.EndReceiveFrom(callback, ref sender);

			while (!cancelUpdatesFromServer)
			{
				int numRead = await Task.Factory.FromAsync(begin, end, null);
				var msg = new byte[numRead];
				Array.Copy(buffer, msg, numRead);
				serverCommands.Enqueue(ServerCommand.Decode(msg));
			}
			updatesFromServer.Shutdown(SocketShutdown.Both);
			updatesFromServer.Close();
		}

		int playerID;
		/// <summary>
		/// Server's address where to send client updates.
		/// </summary>
		IPEndPoint sAddress;
		/// <summary>
		/// Socket for finishing up the connecting process to the server.
		/// ReceivesConnectingDynamicData.
		/// </summary>
		Socket serverDynamic;
		/// <summary>
		/// Task representing connection process status.
		/// Is completed when ClienDynamicData is received and processed.
		/// </summary>
		Task finishConnecting;

		Socket updatesToServer;
		Socket updatesFromServer;
		/// <summary>
		/// Request to terminate the task listening for server updates
		/// </summary>
		bool cancelUpdatesFromServer = false;
		Queue<ServerCommand> serverCommands;
	}
}
