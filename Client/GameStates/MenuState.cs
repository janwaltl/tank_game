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
		public IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states)
		{
			//Already was in menu

			IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23545);
			ConnectingState state = new ConnectingState(serverAddress);
			//Don't reuse
			if (states.ContainsKey(Game.States.Connecting))
				(states[Game.States.Connecting] as IDisposable)?.Dispose();

			states[Game.States.Connecting] = state;
			return state;
		}
	}
}
