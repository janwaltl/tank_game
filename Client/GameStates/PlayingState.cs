﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Client.GameStates
{
	class PlayingState : IGameState
	{
		public PlayingState(IPEndPoint serverAddress, int playerID)
		{
			this.sAddress = serverAddress;
			this.playerID = playerID;
			server = new Socket(sAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

		}
		public IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states)
		{
			//TODO Implement gamelogic
			//CURENTLY just sends a message as update
			var buffer = Encoding.BigEndianUnicode.GetBytes($"Status update from {playerID}");
			int numSend = server.SendTo(buffer, sAddress);
			Debug.Assert(numSend == buffer.Length);
			return this;
		}
		int playerID;
		IPEndPoint sAddress;
		Socket server;
	}
}
