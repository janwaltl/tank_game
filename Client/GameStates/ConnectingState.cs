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
			server.Dispose();
		}

		public void OnSwitch()
		{
			server = new Socket(sAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			server.Connect(sAddress);
			//TODO error handling - SocketException

			//TODO error handling - SocketException
			incomingMsg = Communication.TCPReceiveMessageAsync(server).ContinueWith(t => ClientConnecting.Decode(t.Result));
			Console.WriteLine("Connecting to the server...");
		}
		/// <summary>
		/// Connects to the server and downloads the necessary data=map...
		/// When thats finish it switches to PlayingState
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <param name="states">available game states</param>
		/// <returns>Itself or PlayingState if download finished</returns>
		public IGameState UpdateState(double dt)
		{
			if (incomingMsg.IsCompleted)
			{
				//TODO error handling
				//if(incomingMsg.IsFaulted)
				// throw "Failed to downlaod data from the server"

				var msg = incomingMsg.Result;

				Console.WriteLine("Data from the server:");
				Console.WriteLine($"PlayerID: {msg.playerID} \nMessage: {msg.testMsg}");
				Console.WriteLine("Closing the connection.");
				server.Shutdown(SocketShutdown.Both);
				server.Close();
				Console.WriteLine("Switching to playState.");

				return new PlayingState(new IPEndPoint(sAddress.Address, 23546), msg.playerID);
			}
			Console.WriteLine(incomingMsg.IsCompleted);
			return this;
		}


		private Task<ClientConnecting> incomingMsg;
		private IPEndPoint sAddress;
		private Socket server;
	}
}
/*
Connecting protocol:
Little endian
Message length in bytes excluding this			4B
PlayerID										4b
Testing message(unicode)						the rest

*/
