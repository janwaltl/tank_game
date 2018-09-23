using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Engine
{

	/// <summary>
	/// Main logic class of the game, holds the world. Engine accepts actions which modify state of the world.
	/// World can be observed(for rendering...).
	/// </summary>
	public class Engine
	{
		public Engine(World w)
		{
			World = w;
			var pickupsPoints = World.Arena.GetShieldPickups();
			int i = 0;
			foreach (var p in pickupsPoints)
			{
				var coords = CalcPosFromCellCords(p, World.Arena);
				var pos = new Vector3(coords.X, coords.Y, 0.1f);
				World.shieldPickups.Add(i++, new ShieldPickup(pos, true));
			}
		}
		/// <summary>
		/// Server version of engine tick update. Executes passed comands and handles all collisions.
		/// Can trigger PlayerHit event.
		/// </summary>
		/// <param name="commands"></param>
		/// <param name="dt"></param>


		/// <summary>
		/// Post-physics execution of the PlayerDeathCmds.
		/// </summary>
		public void ServerExecDeathCmds(IEnumerable<EngineCommand> cmds)
		{
			ExecuteCommands(cmds);
		}
		/// <summary>
		/// Client version of engine tick update.
		/// Does not trigger the PlayerHit event.
		/// </summary>
		/// <param name="predictedCmds">Commands to be executed</param>
		public void ClientUpdate(IEnumerable<EngineCommand> predictedCmds, double dt)
		{
			Task.Run(() => Console.ReadLine());
			ExecuteCommands(predictedCmds);
			MoveShells(dt);
			ResolvePlayersArenaCollisions(dt);
			ResolvePlayersInterCollisions(dt);
			ResolveShellCollisions(dt, false);
		}
		/// <summary>
		/// Executes passed commands. Should be used for serverUpdates and predicted but not yet processed commands.
		/// </summary>
		/// <param name="commands"></param>
		public void ClientCatchup(IEnumerable<EngineCommand> commands, double dt)
		{
			ExecuteCommands(commands);
			ResolvePlayersArenaCollisions(dt);
			ResolvePlayersInterCollisions(dt);
		}
		public void ExecuteCommands(IEnumerable<EngineCommand> commands)
		{
			foreach (var c in commands)
				ExecCommand(c);
		}

		/// <summary>
		/// Returns the center of an empty spawn cell's center.
		/// Throw Exception if all spawn points are occupied
		/// </summary>
		/// <returns></returns>
		public Vector2 GetEmptySpawnPoint()
		{
			//Make it a list in order to evaluate it only once.
			var playersCoords = (from p in World.players select CalcCellCoords(p.Value.Position.Xy, World.Arena)).ToList();
			foreach (var s in World.Arena.GetSpawnPoints())
			{
				if (!playersCoords.Contains(s))
					return CalcPosFromCellCords(s, World.Arena);
			}
			throw new Exception("All spawn points are occupied.");
		}
		public World World { get; }
		public delegate void PlayerHitDelegate(Player player, TankShell shell);
		/// <summary>
		/// Will be called in ServerUpdate method as part of collision resolution when a shell hits a player.
		/// The shell will be destroyed and removed from World.shells.
		/// Is NOT called by ClientUpdate method, only ba ServerUpdate
		/// Do NOT add/remove any players, shells in this function.
		/// </summary>
		public event PlayerHitDelegate PlayerHitEvent;
		/// <summary>
		/// Regenerates the shield of players
		/// </summary>
		/// <param name="dt"></param>
		public void RegenShields(double dt)
		{
			shieldRegenAcc += dt;
			if (shieldRegenAcc > 1.0f)//Regenerate each second
			{
				shieldRegenAcc -= 1.0f;
				foreach (var p in World.players.Values)
				{
					if (p.CurrShields > 0 && p.CurrShields < Player.initShields)
						p.CurrShields += Player.shieldRegen;
				}
			}
		}
		/// <summary>
		/// Respawns all shield pickups each 30secs.
		/// Returns whether the shield have been respawned.
		/// </summary>
		public bool RespawnPickups(double dt)
		{
			pickupAcc -= dt;
			if (pickupAcc <= 0.0f)
			{
				foreach (var p in World.shieldPickups)
				{
					p.Value.Active = true;
				}
				pickupAcc += 30.0;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Moves shells according to their velocity.
		/// </summary>
		public void MoveShells(double dt)
		{
			foreach (var s in World.shells)
				s.position += (float)dt * s.Dir * TankShell.shellSpeed;
		}
		/// <summary>
		/// Handles collision between players and arena's walls
		/// </summary>
		/// <param name="dt"></param>
		public void ResolvePlayersArenaCollisions(double dt)
		{
			var a = World.Arena;
			foreach (var p in World.players.Values)
			{
				var cellIndex = CalcCellCoords(p.Position.Xy, a);

				foreach (var cellCenter in GetSurroundingWallCellsCenters(cellIndex, a))
				{
					ResolvePlayerCellColl(p, cellCenter);
				}
			}
		}
		/// <summary>
		/// Handles collisions between shells and Players and the Arena.
		/// Shells are destroyed on impact, playerHits are reported cia event call if enabled.
		/// </summary>
		/// <param name="callEvent">Whether should the player hits trigger PlayerHitEvent.</param>
		public void ResolveShellCollisions(double dt, bool callEvent)
		{
			var a = World.Arena;
			var toBeRemoved = new List<int>();
			void RemoveShell(int index) => toBeRemoved.Add(index);
			bool NotOwner(TankShell shell, Player p) => p.ID != shell.OwnerPID;
			for (int i = 0; i < World.shells.Count; ++i)
			{
				var shell = World.shells[i];
				//Collisions with the players
				bool removed = false;
				foreach (var p in World.players.Values)
					if (ResolveSphereAABBCollsion(p.Position.Xy, Player.radius,
												  shell.position, TankShell.boundingBox).LengthSquared > 0.0
						&& NotOwner(shell, p))//Ignore self-collisions
					{
						if (callEvent)
							PlayerHitEvent?.Invoke(p, shell);
						RemoveShell(i);
						removed = true;
						break;
					}
				if (removed) continue;
				//Collisions with the arena
				foreach (var cellCenter in GetSurroundingWallCellsCenters(CalcCellCoords(shell.position, a), a))
					if (ShellAABBCollisionCheck(shell.position, cellCenter, Arena.boundingBox))
					{
						RemoveShell(i);
						break;
					}
			}

			int end = World.shells.Count;
			//Move 'toBeRemoved' elements to the end of 
			for (int i = toBeRemoved.Count - 1; i >= 0; --i)
			{
				World.shells[i] = World.shells[end - 1];
				--end;
			}
			if (end < World.shells.Count)
				World.shells.RemoveRange(end, World.shells.Count - end);
		}
		/// <summary>
		/// Handles collisions between individual players.
		/// </summary>
		/// <param name="dt"></param>
		public void ResolvePlayersInterCollisions(double dt)
		{
			foreach (var p1 in World.players.Values)
			{
				foreach (var p2 in World.players.Values)
				{
					if (!ReferenceEquals(p1, p2))
					{
						var sepVec = ResolveSphereSphereCollision(p1.Position.Xy, Player.radius, p2.Position.Xy, Player.radius);
						p1.Position += new Vector3(sepVec.X, sepVec.Y, 0.0f) / 2.0f;
						p2.Position -= new Vector3(sepVec.X, sepVec.Y, 0.0f) / 2.0f;
					}
				}
			}
		}
		/// <summary>
		/// Returns list of players that picked up shields.
		/// Restores shields for such players and despawns picked-up shields.
		/// </summary>
		public List<Tuple<Player, int>> ResolvePlayerPickupCollisions(double dt)
		{
			var list = new List<Tuple<Player, int>>();
			foreach (var p in World.players.Values)
			{
				foreach (var s in World.shieldPickups)
				{
					if (s.Value.Active)
					{
						var res = ResolveSphereSphereCollision(p.Position.Xy, Player.radius, s.Value.pos.Xy, Player.radius);
						if (res.LengthSquared > 0)
						{
							list.Add(new Tuple<Player, int>(p, s.Key));
							//Despawn it
							s.Value.Active = false;
							//Restore the shields.
							p.CurrShields = Player.initShields;
						}
					}
				}
			}
			return list;
		}
		/// <summary>
		/// Returns 8 sorrounding cells + the center cell of type 'Wall'.
		/// May return fewer if the cellIndex is near the sides/corners of the arena.
		/// </summary>
		/// <param name="cellIndex">(x,y) indices of the center cells</param>
		/// <returns></returns>
		IEnumerable<Vector2> GetSurroundingWallCellsCenters(Arena.Coords cell, Arena a)
		{
			for (int yDelta = -1; yDelta <= 1; ++yDelta)
				for (int xDelta = -1; xDelta <= 1; ++xDelta)
				{
					int x = cell.x + xDelta;
					int y = cell.y + yDelta;
					if (x >= 0 && y >= 0 && x < a.Size && y < a.Size && a[x, y] == Arena.CellType.wall)
						yield return CalcPosFromCellCords(new Arena.Coords(x, y), a);
				}
		}
		/// <summary>
		/// Checks if a shell is colliding with an AABB.
		/// </summary>
		/// <returns>Whether the collision occured.</returns>
		bool ShellAABBCollisionCheck(Vector2 shellPosition, Vector2 aabbCenter, Vector2 aabbDims)
		{
			var sepDist = shellPosition - aabbCenter;
			sepDist.X = Math.Abs(sepDist.X);
			sepDist.Y = Math.Abs(sepDist.Y);

			var dims = (TankShell.boundingBox + aabbDims) / 2.0f;
			return sepDist.X - dims.X < 0.0f && sepDist.Y - dims.Y < 0.0f;
		}
		/// <summary>
		/// Calculates (x,y) indices that represent an Arena's cell in which is the position is located.
		/// </summary>
		static Arena.Coords CalcCellCoords(Vector2 position, Arena a)
		{
			//Calculate index of the cell where the player's center is.
			var cellIndexF = (position - Arena.origin) * Arena.offset + Arena.boundingBox / 2.0f;
			cellIndexF.X /= Arena.boundingBox.X;
			cellIndexF.Y /= Arena.boundingBox.Y;
			//Should not happen
			if (cellIndexF.X < 0.0f || cellIndexF.Y < 0.0f ||
				cellIndexF.X >= a.Size || cellIndexF.X >= a.Size)
				Console.WriteLine("Something escaped the arena.");

			return new Arena.Coords((int)cellIndexF.X, (int)cellIndexF.Y);
		}
		static Vector2 CalcPosFromCellCords(Arena.Coords coords, Arena a)
		{
			return Arena.origin + new Vector2(coords.x, coords.y) * Arena.offset * Arena.boundingBox;
		}
		/// <summary>
		/// Cooldown for shieldRegen.
		/// </summary>
		double shieldRegenAcc;
		/// <summary>
		/// Cooldown for shieldPickups.
		/// </summary>
		double pickupAcc;
		void ExecCommand(EngineCommand c)
		{
			c.Execute(World);
		}
		/// <summary>
		/// Resolves AABB collision between the player and cell.
		/// </summary>
		static void ResolvePlayerCellColl(Player p, Vector2 cellCenter)
		{
			Vector2 sepVec = ResolveSphereAABBCollsion(p.Position.Xy, Player.radius, cellCenter, Arena.boundingBox);
			p.Position += new Vector3(sepVec.X, sepVec.Y, 0.0f);
		}

		/// <summary>
		/// Resolves collision between two spheres, returned vector should be added to first's position to separate it from the second.
		/// Returns 0 vector if there was no collision.
		/// </summary>
		/// <returns>Separation vertor = applying it to first's position seperates it from the second AABB.
		/// Returns 0 vector if there was no collision.</returns>
		static Vector2 ResolveSphereSphereCollision(Vector2 firstCenter, float firstR, Vector2 secondCenter, float secondR)
		{
			var dir = firstCenter - secondCenter;
			var dist2 = dir.LengthSquared;
			var radii = firstR + secondR;
			if (radii * radii < dist2)
				return new Vector2();

			var penDepth = (float)(radii - Math.Sqrt(dist2));

			return dir.Normalized() * penDepth;
		}
		/// <summary>
		/// Resolves collision between sphere and a AABB, returned vector should be added to sphere's position to separate it from the AABB.
		/// Returns 0 vector if there was no collision.
		/// </summary>
		/// <returns>Separation vertor = applying it to sphere's position seperates it from the AABB.
		/// Returns 0 vector if there was no collision.</returns>
		static Vector2 ResolveSphereAABBCollsion(Vector2 sphereCenter, float sphereR, Vector2 AABBCenter, Vector2 AABBDims)
		{
			var closestP = Vector2.Clamp(sphereCenter, AABBCenter - AABBDims / 2.0f, AABBCenter + AABBDims / 2.0f);

			var dir = sphereCenter - closestP;
			var dist2 = dir.LengthSquared;
			if (dist2 > sphereR * sphereR)
				return new Vector2();

			var penDepth = (float)(sphereR - Math.Sqrt(dist2));
			return dir.Normalized() * penDepth;
		}
	}
}
