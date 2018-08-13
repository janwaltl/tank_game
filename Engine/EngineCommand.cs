using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public abstract class EngineCommand
	{
		public abstract void Execute(World p);
	}
	/// <summary>
	/// Sets players' states when executed.
	/// </summary>
	public class PlayersStateCommand : EngineCommand
	{
		public struct PlayerState
		{
			public int playerID;
			public Vector3 pos;
			//Vector3 vel;
			//float angle;
		}
		public PlayersStateCommand(List<PlayerState> playerStates)
		{
			this.playerStates = playerStates;
		}
		public override void Execute(World p)
		{
			foreach (var pS in playerStates)
			{
				//RESOLVE Probably should ignore "invalid" command
				// State packet might arrive before 'playerCOnnected' packet 
				// or after 'playerDisconnected'
				// this would be update for yet non-existing player.
				if (p.players.ContainsKey(pS.playerID))
					p.players[pS.playerID].Position = pS.pos;
			}
		}
		readonly List<PlayerState> playerStates;
	}
}
