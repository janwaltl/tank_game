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
			public PlayerState(int pID, Vector3 pPos, Vector3 pVel)
			{
				playerID = pID;
				pos = pPos;
				vel = pVel;
			}
			public int playerID;
			public Vector3 pos;
			public Vector3 vel;
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
				{
					p.players[pS.playerID].Position = pS.pos;
					p.players[pS.playerID].Velocity = pS.vel;
				}
			}
		}
		readonly List<PlayerState> playerStates;
	}
	public class PlayerConnectedCmd : EngineCommand
	{
		public PlayerConnectedCmd(int pID, Vector3 pCol, Vector3 pPos, Vector3 pVel)
		{
			this.pID = pID;
			this.pCol = pCol;
			this.pPos = pPos;
			this.pVel = pVel;
		}
		public override void Execute(World p)
		{
			p.players.Add(pID, new Player(pID, pPos, pVel, pCol));
		}
		int pID;
		Vector3 pCol, pPos, pVel;
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
	/// <summary>
	/// When executed changes player's velocity.
	/// </summary>
	public class PlayerAccCmd : EngineCommand
	{
		/// <param name="deltaVel">When executed this amount will be added to player's current velocity.</param>
		public PlayerAccCmd(int playerID, Vector3 deltaVel)
		{
			pID = playerID;
			this.deltaVel = deltaVel;
		}
		public override void Execute(World p)
		{
			p.players[pID].Velocity += deltaVel;
		}
		int pID;
		Vector3 deltaVel;
	}


}
