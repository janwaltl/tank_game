using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
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
			firstUpdate = true;
		}

		public void Dispose()
		{
			s.Dispose();
		}

		public IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states)
		{
			if (firstUpdate)//Connect to the server
			{
				firstUpdate = false;
				s = new Socket(sAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				s.Connect(sAddress);

				return this;
			}
			else if (s.Available > 0)
			{
				//TODO Collect data from the server
				// - map, playerID, actions...
				// - if collected, end the connection, return playing state
			}

			return this;
		}

		private bool firstUpdate;
		private IPEndPoint sAddress;
		private Socket s;
	}
}
