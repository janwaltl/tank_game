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
		public MenuState(Input input)
		{
			this.input = input;
		}
		public void Dispose() { }

		public void OnSwitch() { }
		public void RenderState(double dt)
		{
			//TODO Render simple menu.
		}
		/// <summary>
		/// Immidietely switches to connecting state.
		/// </summary>
		/// <returns>ConnectingState</returns>
		public IGameState UpdateState(double dt)
		{
			//TEMP server IP address
			IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Ports.serverConnection);
			var sManager = new ServerManager(serverAddress);
			return new PlayingState(sManager, input);
		}
		private Input input;
	}
}
