using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Shared;
namespace Client.GameStates
{
	/// <summary>
	/// Main menu of the game.
	/// CURRENTLY it immidietely tries to connect to the server.
	/// </summary>
	class MenuState : IGameState
	{
		public MenuState(Input input, IPAddress serverAddress)
		{
			this.serverAddress = new IPEndPoint(serverAddress, Ports.serverConnection);
			this.input = input;
		}
		public void Dispose() { }

		public void OnSwitch() { }
		public void RenderState(double dt)
		{}
		/// <summary>
		/// Immidietely switches to connecting state.
		/// </summary>
		/// <returns>ConnectingState</returns>
		public IGameState UpdateState(double dt)
		{
			var sManager = new ServerManager(serverAddress);
			return new PlayingState(sManager, input);
		}
		private IPEndPoint serverAddress;
		private Input input;
	}
}
