using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
			s.Dispose();
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
				s = new Socket(sAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				s.Connect(sAddress);
				//TODO error handling - SocketException

				incomingMsg = RecieveMsgAsync(s);
				Console.WriteLine("Connecting to the server...");
				return this;
			}
			else if (incomingMsg.IsCompleted)
			{
				//TODO error handling
				//if(incomingMsg.IsFaulted)
				// throw "Failed to downlaod data from the server"
				var msg = incomingMsg.Result.ToArray();

				int playerID = BitConverter.ToInt32(msg, 4);
				string textMsg = Encoding.BigEndianUnicode.GetString(msg, 8, msg.Length - 8);
				Console.WriteLine("Data from the server:");
				Console.WriteLine($"PlayerID: {playerID} \nMessage: {textMsg}");
				Console.WriteLine("Closing the connection.");
				s.Shutdown(SocketShutdown.Both);
				s.Close();
				Console.WriteLine("Switching to playState.");
				//CURRENTLY Do not recycle, probably worth it int the future because of the rendering and so on
				if (states.ContainsKey(Game.States.Playing))
					(states[Game.States.Playing] as IDisposable)?.Dispose();
				return states[Game.States.Playing] = new PlayingState();
			}
			Console.WriteLine(incomingMsg.IsCompleted);
			return this;
		}
		/// <summary>
		/// Downloads data from the server
		/// </summary>
		/// <param name="s">Connected socket to the server</param>
		/// <returns></returns>
		private async Task<List<byte>> RecieveMsgAsync(Socket server)
		{
			byte[] buffer = new byte[1024];
			//Make correct signature for Task.Factory
			Func<AsyncCallback, object, IAsyncResult> begin = (callback, state) => s.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, state);
			int msgLength = 4;//Atleast four because that specifies true length
			List<byte> msg = new List<byte>();
			do
			{
				int numRead = await Task.Factory.FromAsync(begin, s.EndReceive, null);
				if (numRead == 0)//RESOLVE proper error checking
					throw new NotImplementedException("Connection has been closed by the server.");
				var byteMsg = new byte[numRead];
				Array.Copy(buffer, byteMsg, numRead);
				msg.AddRange(byteMsg);
				if (msg.Count >= 4)//True length has been recieved
				{
					var trueLenBytes = BitConverter.IsLittleEndian ?
						new byte[4] { msg[0], msg[1], msg[2], msg[3] } :
						new byte[4] { msg[3], msg[2], msg[1], msg[0] };
					msgLength = BitConverter.ToInt32(trueLenBytes, 0);
				}
			} while (msg.Count < msgLength);

			Debug.Assert(msg.Count == msgLength);
			return msg;
		}

		private Task<List<byte>> incomingMsg;
		private IPEndPoint sAddress;
		private Socket s;
	}
}
/*
Connecting protocol:
Little endian
Message length in bytes including this			4B
PlayerID										4b
Testing message(unicode)						the rest

*/
