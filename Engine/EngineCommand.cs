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
			public PlayerState(int pID, Vector3 pPos)
			{
				playerID = pID;
				pos = pPos;
			}
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
				//Ignore "race conditions" for packets
				// State packet might arrive before 'playerCOnnected' packet 
				// or after 'playerDisconnected'
				// this would be update for yet non-existing player.
				if (p.players.ContainsKey(pS.playerID))
					p.players[pS.playerID].Position = pS.pos;
			}
		}
		readonly List<PlayerState> playerStates;
	}
	public class PlayerConnectedCmd : EngineCommand
	{
		public PlayerConnectedCmd(int pID, Vector3 pCol, Vector3 pPos)
		{
			this.pID = pID;
			this.pCol = pCol;
			this.pPos = pPos;
		}
		public override void Execute(World p)
		{
			p.players.Add(pID, new Player(pID, pPos, pCol));
		}
		int pID;
		Vector3 pCol, pPos;
	}
	public class PlayerDisconnectedCmd : EngineCommand
	{
		public PlayerDisconnectedCmd(int playerID)
		{
			pID = playerID;
		}
		public override void Execute(World p)
		{
			p.players.Remove(pID);
		}
		int pID;
	}
	public class PlayerMoveCmd : EngineCommand
	{
		public PlayerMoveCmd(int playerID, Vector3 moveOffset)
		{
			pID = playerID;
			offset = moveOffset;
		}
		public override void Execute(World p)
		{
			p.players[pID].Position += offset;
		}
		int pID;
		Vector3 offset;
	}


}
