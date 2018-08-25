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
			public PlayerState(int pID, Vector3 pPos, Vector3 pVel, float towerAngle)
			{
				playerID = pID;
				pos = pPos;
				vel = pVel;
				this.towerAngle = towerAngle;
			}
			public int playerID;
			public Vector3 pos;
			public Vector3 vel;
			public float towerAngle;
		}
		public PlayersStateCommand(List<PlayerState> playerStates)
		{
			this.playerStates = playerStates;
		}
		public override void Execute(World world)
		{
			foreach (var pS in playerStates)
			{
				//Ignore "race conditions" for packets
				// State packet might arrive before 'playerCOnnected' packet 
				// or after 'playerDisconnected'
				// this would be update for yet non-existing player.
				if (world.players.ContainsKey(pS.playerID))
				{
					world.players[pS.playerID].Position = pS.pos;
					world.players[pS.playerID].Velocity = pS.vel;
					world.players[pS.playerID].TowerAngle = pS.towerAngle;
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
		public override void Execute(World world)
		{
			world.players.Add(pID, new Player(pID, pPos, pVel, pCol));
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
		public override void Execute(World world)
		{
			world.players.Remove(pID);
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
		public override void Execute(World world)
		{
			world.players[pID].Velocity += deltaVel;
		}
		int pID;
		Vector3 deltaVel;
	}
	/// <summary>
	/// When executes sets player's tank tower's angle.
	/// </summary>
	public class PlayerTowerCmd : EngineCommand
	{
		public PlayerTowerCmd(int playerID, float newTowerAngle)
		{
			pID = playerID;
			angle = newTowerAngle;
		}
		public override void Execute(World world)
		{
			world.players[pID].TowerAngle = angle;
		}
		int pID;
		float angle;
	}
	public class PlayerShootCmd : EngineCommand
	{
		public PlayerShootCmd(int playerID)
		{
			pID = playerID;
		}
		public override void Execute(World world)
		{
			if (world.players.TryGetValue(pID, out Player player))
			{
				//Shift to have zero angle=(1,0) dir
				var shootingAngle = player.TowerAngle - MathHelper.PiOver2;
				var dir = new Vector2((float)Math.Cos(shootingAngle), (float)Math.Sin(shootingAngle));
				var pos = player.Position.Xy;
				world.shells.Add(new TankShell(dir, pos, pID));
				Console.WriteLine("Shooting at {0}", dir);
			}
		}
		int pID;
	}
}
