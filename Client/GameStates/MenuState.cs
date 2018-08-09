﻿using System;
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
	class MenuState : GameState
	{
		public override void Dispose() { }

		public override void OnSwitch() { }
		public override void RenderState(double dt)
		{
			//TODO Render simple menu.
		}
		/// <summary>
		/// Immidietely switches to connecting state.
		/// </summary>
		/// <returns>ConnectingState</returns>
		public override GameState UpdateState(double dt)
		{
			//TEMP server IP address
			IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Ports.serverConnection);
			ConnectingState state = new ConnectingState(serverAddress);
			return state;
		}
	}
}
