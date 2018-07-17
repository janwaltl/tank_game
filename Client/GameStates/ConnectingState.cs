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
		/// <summary>
		/// Connects to the server and downloads the necessary data=map...
		/// When thats finish it switches to PlayingState
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <param name="states">available game states</param>
		/// <returns>Itself or PlayingState if download finished</returns>
		public IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states)
		{
			if (incomingMsg == null)//Connect to the server
			{
				server = new Socket(sAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				server.Connect(sAddress);
				//TODO error handling - SocketException

				incomingMsg = RecieveMsgAsync(server);
				Console.WriteLine("Connecting to the server...");
				return this;
			}
			else if (incomingMsg.IsCompleted)
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

				//CURRENTLY Do not recycle, probably worth it int the future because of the rendering and so on
				if (states.ContainsKey(Game.States.Playing))
					(states[Game.States.Playing] as IDisposable)?.Dispose();
				return states[Game.States.Playing] = new PlayingState(new IPEndPoint(sAddress.Address, 23546), msg.playerID);
			}
			Console.WriteLine(incomingMsg.IsCompleted);
			return this;
		}
		/// <summary>
		/// Downloads data from the server using TCP socket.
		/// </summary>
		/// <param name="s">Connected socket to the server</param>
		/// <returns></returns>
		public static async Task<ClientConnecting> RecieveMsgAsync(Socket server)
		{
			byte[] buffer = new byte[1024];
			//Make correct signature for Task.Factory
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => server.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, state);
			int msgLength = 4;//Atleast four because that specifies true length
			List<byte> msg = new List<byte>();
			do
			{
				int numRead = await Task.Factory.FromAsync(begin, server.EndReceive, null);
				if (numRead == 0)//RESOLVE proper error checking
					throw new NotImplementedException("Connection has been closed by the server.");

				var byteMsg = new byte[numRead];
				Array.Copy(buffer, byteMsg, numRead);
				msg.AddRange(byteMsg);

				if (msg.Count >= 4)//True length has been recieved
					msgLength = Serialization.DecodeInt(new byte[4] { msg[0], msg[1], msg[2], msg[3] }, 0) + 4;//+4for the initial heade
			} while (msg.Count < msgLength);

			Debug.Assert(msg.Count == msgLength);
			return ClientConnecting.Decode(msg.ToArray(), 4);
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
