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
		public PlayingState(IPEndPoint serverAddress, ConnectingStaticData sData, Socket server, Input input)
		{
			this.sAddress = serverAddress;
			this.playerID = sData.playerID;
			this.serverDynamic = server;
			this.input = input;
			serverCommands = new Queue<ServerCommand>();

			BuildEngine(sData);
			renderer = new Playing.Renderer(input, engine);
		}
		public IGameState UpdateState(double dt)
		{
			//Wait for the dynamic data
			//RESOLVE faulted status
			if (finishConnecting.Status == TaskStatus.RanToCompletion)
			{
				SendClientupdate(dt);
				var commands = ProcessServerCommands();
				engine.ExecuteCommands(commands);
				//TEMP Do not rung Physics
			}
			return this;
		}


		public void OnSwitch()
		{
			updatesToServer = new Socket(sAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			//Start listening for commands
			//TEMP Shift ports by playerID=Allows multiple clients on one computer
			var listenOn = new IPEndPoint(IPAddress.Loopback, Ports.serverUpdates + playerID);
			Task.Run(() => ListenForServerCommandsAsync(listenOn)).Detach();
			//Finish the connecting process
			finishConnecting = FinishConnecting();
		}
		public void RenderState(double dt)
		{
			renderer.Render(dt);
		}
		public void Dispose()
		{
			updatesToServer.Shutdown(SocketShutdown.Both);
			updatesToServer.Close();
			cancelUpdatesFromServer = true;
		}
		private void BuildEngine(ConnectingStaticData sData)
		{
			//TODO build world according to received sData.
			var world = new Engine.World(new Engine.Arena(10));
			this.engine = new Engine.Engine(world);
		}
		private async Task FinishConnecting()
		{
			//Confirm that static part received and started listening for commands.
			await Communication.TCPSendACKAsync(serverDynamic);
			Console.WriteLine("Sent server ACK.");
			var data = ConnectingDynamicData.Decode(await Communication.TCPReceiveMessageAsync(serverDynamic));
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
		private void SendClientupdate(double dt)
		{
			ClientUpdate.PressedKeys pressed = ClientUpdate.PressedKeys.None;
			//Polls input
			if (input.IsKeyPressed(OpenTK.Input.Key.W))
				pressed |= ClientUpdate.PressedKeys.W;
			if (input.IsKeyPressed(OpenTK.Input.Key.A))
				pressed |= ClientUpdate.PressedKeys.A;
			if (input.IsKeyPressed(OpenTK.Input.Key.S))
				pressed |= ClientUpdate.PressedKeys.S;
			if (input.IsKeyPressed(OpenTK.Input.Key.D))
				pressed |= ClientUpdate.PressedKeys.D;
			var cU = new ClientUpdate(playerID, pressed, dt);
			//Sends the update, does not wait for it
			Communication.UDPSendMessageAsync(updatesToServer, sAddress, ClientUpdate.Encode(cU)).Detach();
		}

		private List<Engine.EngineCommand> ProcessServerCommands()
		{
			var queue = Interlocked.Exchange(ref serverCommands, new Queue<ServerCommand>());
			if (queue.Count > 0)
				Console.WriteLine($"ServerCommands({queue.Count}):");
			List<Engine.EngineCommand> commands = new List<Engine.EngineCommand>();
			foreach (var item in queue)
				commands.Add(item.Translate());
			return commands;
		}
		/// <summary>
		/// Starts listesting for server updates, any received updates are pushed into serverCommands queue.
		/// </summary>
		/// <param name="listenOn">local IP+port on which should the client listen for the updates.</param>
		/// <returns>when server ends the connection or <paramref name="cancelUpdatesFromServer"/>'' is set.</returns>
		private async Task ListenForServerCommandsAsync(IPEndPoint listenOn)
		{
			updatesFromServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			updatesFromServer.Bind(listenOn);
			Console.WriteLine($"Started listening for server updates on {listenOn}");
			while (!cancelUpdatesFromServer)
			{
				var msg = await Communication.UDPReceiveMessageAsync(updatesFromServer, 1024);

				serverCommands.Enqueue(ServerCommand.Decode(msg.Item1));
			}
			updatesFromServer.Shutdown(SocketShutdown.Both);
			updatesFromServer.Close();
		}

		readonly int playerID;
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

		readonly Input input;
		Engine.Engine engine;
		Playing.Renderer renderer;
	}
}
