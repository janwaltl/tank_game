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
	abstract class GameState : IDisposable
	{

		/// <summary>
		/// Is run by Game when the state is set as Active
		/// </summary>
		public abstract void OnSwitch();
		/// <summary>
		/// Updates the state.
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <returns>Next active state. Return null to exit the application.</returns>
		public abstract GameState UpdateState(double dt);

		/// <summary>
		/// Renders the state
		/// </summary>
		/// <param name="dt">delta time</param>
		public abstract void RenderState(double dt);
		public abstract void Dispose();
	}
}
