using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace Client.GameStates
{
	/// <summary>
	/// Main menu of the game.
	/// CURRENTLY it immidietely tries to connect to the server.
	/// </summary>
	class MenuState : IGameState
	{
		public void Dispose() { }

		public void OnSwitch() { }

		public IGameState UpdateState(double dt)
		{
			//Already was in menu

			IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23545);
			ConnectingState state = new ConnectingState(serverAddress);
			return state;
		}
	}
}
