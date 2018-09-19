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
			public PlayerState(int pID, Vector3 pPos, float towerAngle, float tankAngle, double fireCooldown)
			{
				playerID = pID;
				pos = pPos;
				this.towerAngle = towerAngle;
				this.tankAngle = tankAngle;
				this.fireCooldown = fireCooldown;
			}
			public int playerID;
			public Vector3 pos;
			public float towerAngle;
			public float tankAngle;
			public double fireCooldown;
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
					world.players[pS.playerID].TankAngle = pS.tankAngle;
					world.players[pS.playerID].TowerAngle = pS.towerAngle;
					world.players[pS.playerID].CurrFireCooldown = pS.fireCooldown;
				}
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
		public override void Execute(World world)
		{
			world.players.Add(pID, new Player(pID, pPos, pCol));
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
		public override void Execute(World world)
		{
			world.players.Remove(pID);
		}
		int pID;
	}
	/// <summary>
	/// When executed changes player's position.
	/// </summary>
	public class PlayerAccCmd : EngineCommand
	{
		/// <param name="deltaPos">When executed this amount will be added to player's current position.</param>
		public PlayerAccCmd(int playerID, Vector3 deltaPos)
		{
			pID = playerID;
			dPos = deltaPos;
		}
		public override void Execute(World world)
		{
			if (world.players.TryGetValue(pID, out Player p))
			{
				p.Position += dPos;
				if (dPos.LengthSquared > 0.0)
					p.TankAngle = (float)(Math.Atan2(dPos.Y, dPos.X) + Math.PI / 2.0);
			}
		}
		int pID;
		Vector3 dPos;
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
			if (world.players.TryGetValue(pID, out Player player))
				player.TowerAngle = angle;
		}
		int pID;
		float angle;
	}
	/// <summary>
	/// Fires a new shell from player's tank.
	/// </summary>
	public class PlayerFireCmd : EngineCommand
	{
		public PlayerFireCmd(int playerID, Vector2 shootingDir, Vector2 shootingPos)
		{
			pID = playerID;
			sDir = shootingDir;
			sPos = shootingPos;
		}
		public override void Execute(World world)
		{
			if (world.players.TryGetValue(pID, out Player player))
			{
				//Spawn the shell at the edge of the player.
				var pos = sPos + sDir * Player.radius;
				world.shells.Add(new TankShell(sDir, pos, pID));
			}
		}
		Vector2 sDir;
		Vector2 sPos;
		int pID;
	}
}
