using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Shared;
namespace Client.GameStates
{
	/// <summary>
	/// GameState that represents the game connecting to the server
	/// </summary>
	class ConnectingState : IGameState, IDisposable
	{
		public ConnectingState(IPEndPoint serverAddress)
		{
			sAddress = serverAddress;
		}

		public void Dispose()
		{
			server?.Dispose();
		}

		public void OnSwitch()
		{
			server = new Socket(sAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			server.Connect(sAddress);
			//TODO error handling - SocketException

			//TODO error handling - SocketException
			staticData = ReceivedStaticDataAsync();
			Console.WriteLine("Connecting to the server...");
		}
		public void RenderState(double dt)
		{
			//TODO progress bar?
		}
		/// <summary>
		/// Connects to the server and downloads the necessary static data=map...
		/// When thats finished switches to PlayingState.
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <param name="states">available game states</param>
		/// <returns>Itself or PlayingState if the data has been received.</returns>
		public IGameState UpdateState(double dt)
		{
			//TODO error handling
			//if(staticData.IsFaulted)
			// throw "Failed to downlaod data from the server"
			if (staticData.IsCompleted)
			{
				var sData = staticData.Result;
				Console.WriteLine("Received static data from the server.");
				Console.WriteLine("Switching to playState.");

				var newState = new PlayingState(new IPEndPoint(sAddress.Address, Ports.clientUpdates), sData, server);
				server = null;//Release ownership of this socket so it won't get disposed with this instance.
				return newState;
			}
			Console.WriteLine(staticData.IsCompleted);
			return this;
		}

		public async Task<ConnectingStaticData> ReceivedStaticDataAsync()
		{
			return ConnectingStaticData.Decode(await Communication.TCPReceiveMessageAsync(server));
		}

		private Task<ConnectingStaticData> staticData;
		/// <summary>
		/// Server address
		/// </summary>
		private IPEndPoint sAddress;
		private Socket server;
	}
}
