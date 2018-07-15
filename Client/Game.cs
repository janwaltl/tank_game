using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.GameStates;

namespace Client
{
	/// <summary>
	/// Encapsulases the game as a state machine.
	/// </summary>
	class Game
	{
		public enum States
		{
			Menu,
			Connecting,
			Spectaing,
			Playing,
		}
		public Game(IGameState menuState)
		{
			states = new Dictionary<States, IGameState>();
			states.Add(States.Menu, menuState);
			activeState = menuState;
		}
		/// <summary>
		/// Updates the game
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <returns>Whether the game wants to continue. <see langword="false"/> means exit.</returns>
		public bool Update(double dt)
		{
			activeState = activeState.UpdateState(dt, states);
			if (activeState == null)
				return false;

			Debug.Assert(states.ContainsValue(activeState));
			return true;
		}

		IGameState activeState;
		Dictionary<States, IGameState> states;
	}
}
