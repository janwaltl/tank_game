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
	class Game : IDisposable
	{
		public Game(IGameState menuState)
		{
			activeState = menuState;
		}
		/// <summary>
		/// Updates the game
		/// </summary>
		/// <param name="dt">delta time</param>
		/// <returns>Whether the game wants to continue. <see langword="false"/> means exit.</returns>
		public bool Update(double dt)
		{
			var newState = activeState.UpdateState(dt);
			if (newState == null)
				return false;
			if (!ReferenceEquals(newState, activeState))
			{
				newState.OnSwitch();
				activeState.Dispose();
			}
			activeState = newState;

			return true;
		}
		public void Render(double dt)
		{
			activeState.RenderState(dt);
		}
		public void Dispose()
		{
			activeState?.Dispose();
		}

		IGameState activeState;
	}
}
