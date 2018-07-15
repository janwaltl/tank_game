using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.GameStates
{
	/// <summary>
	/// Represent a state of the game's state machine.
	/// </summary>
	interface IGameState
	{
		/// <summary>
		/// Updates the state.
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <param name="states">List of available states</param>
		/// <returns>Next active state, must be from the list. So a new state must be added there.
		/// Return null to exit the application.</returns>
		IGameState UpdateState(double dt, Dictionary<Game.States, IGameState> states);
	}
}
