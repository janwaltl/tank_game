using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.GameStates
{
	class PlayingState : IGameState
	{
		public IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states)
		{
			//TODO implement
			return this;
		}
	}
}
