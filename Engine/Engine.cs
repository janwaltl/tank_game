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
		}
		/// <summary>
		/// Server version of engine tick update. Executes passed comands and handles all collisions.
		/// Can trigger PlayerHit event.
		/// </summary>
		/// <param name="commands"></param>
		/// <param name="dt"></param>
		public void ServerUpdate(IEnumerable<EngineCommand> commands, double dt)
		{
			ExecuteCommands(commands);
			MoveShells(dt);

			ResolvePlayersArenaCollisions(dt);
			ResolvePlayersInterCollisions(dt);
			ResolveShellCollisions(dt, true);
		}
		/// <summary>
		/// Client version of engine tick update.
		/// Does not trigger the PlayerHit event.
		/// </summary>
		/// <param name="predictedCmds">Commands to be executed</param>
		public void ClientUpdate(IEnumerable<EngineCommand> predictedCmds, double dt)
		{
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
		public void ClientCatchup(IEnumerable<EngineCommand> commands,double dt)
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
		public World World { get; }
		public delegate void PlayerHitDelegate(Player player, TankShell shell);
		/// <summary>
		/// Will be called in ServerUpdate method as part of collision resolution when a shell hits a player.
		/// The shell will be destroyed and removed from World.shells.
		/// Is NOT called by ClientUpdate method, only ba ServerUpdate
		/// Do NOT add/remove any players, shells in this function.
		/// </summary>
		public event PlayerHitDelegate PlayerHitEvent;
		void ExecCommand(EngineCommand c)
		{
			c.Execute(World);
		}
		/// <summary>
		/// Moves shells according to their velocity.
		/// </summary>
		void MoveShells(double dt)
		{
			foreach (var s in World.shells)
				s.position += (float)dt * s.Dir * TankShell.shellSpeed;
		}
		/// <summary>
		/// Handles collision between players and arena's walls
		/// </summary>
		/// <param name="dt"></param>
		void ResolvePlayersArenaCollisions(double dt)
		{
			var a = World.Arena;
			foreach (var p in World.players.Values)
			{
				var cellIndex = CalcCellIndex(p.Position.Xy, a);

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
		void ResolveShellCollisions(double dt, bool callEvent)
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
				foreach (var cellCenter in GetSurroundingWallCellsCenters(CalcCellIndex(shell.position, a), a))
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
		/// Returns 8 sorrounding cells + the center cell of type 'Wall'.
		/// May return fewer if the cellIndex is near the sides/corners of the arena.
		/// </summary>
		/// <param name="cellIndex">(x,y) indices of the center cells</param>
		/// <returns></returns>
		IEnumerable<Vector2> GetSurroundingWallCellsCenters(Tuple<int, int> cellIndex, Arena a)
		{
			for (int yDelta = -1; yDelta <= 1; ++yDelta)
				for (int xDelta = -1; xDelta <= 1; ++xDelta)
				{
					int x = cellIndex.Item1 + xDelta;
					int y = cellIndex.Item2 + yDelta;
					if (x >= 0 && y >= 0 && x < a.Size && y < a.Size && a[x, y] == Arena.CellType.wall)
						yield return Arena.origin + new Vector2(x, y) * Arena.offset * Arena.boundingBox;
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
		/// Handles collisions between individual players.
		/// </summary>
		/// <param name="dt"></param>
		void ResolvePlayersInterCollisions(double dt)
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
		/// Calculates (x,y) indices that represent an Arena's cell in which is the position is located.
		/// </summary>
		static Tuple<int, int> CalcCellIndex(Vector2 position, Arena a)
		{
			//Calculate index of the cell where the player's center is.
			var cellIndexF = (position - Arena.origin) * Arena.offset + Arena.boundingBox / 2.0f;
			cellIndexF.X /= Arena.boundingBox.X;
			cellIndexF.Y /= Arena.boundingBox.Y;
			//Should not happen
			if (cellIndexF.X < 0.0f || cellIndexF.Y < 0.0f ||
				cellIndexF.X >= a.Size || cellIndexF.X >= a.Size)
				Console.WriteLine("Something escaped the arena.");

			return new Tuple<int, int>((int)cellIndexF.X, (int)cellIndexF.Y);
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
