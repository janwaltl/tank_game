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
	interface IGameState: IDisposable
	{

		/// <summary>
		/// Is run by Game when the state is set as Active
		/// </summary>
		void OnSwitch();
		/// <summary>
		/// Updates the state.
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <returns>Next active state. Return null to exit the application.</returns>
		IGameState UpdateState(double dt);

	}
}
