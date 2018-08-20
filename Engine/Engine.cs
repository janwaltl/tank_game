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

		public void ExecuteCommands(IEnumerable<EngineCommand> commands)
		{
			foreach (var c in commands)
				ExecCommand(c);
		}
		/// <summary>
		/// Executes one step of the physics simulation
		/// </summary>
		/// <param name="dt">Delta time in seconds.</param>
		public void RunPhysics(double dt)
		{
			MovePlayers(dt);

			ResolvePlayersArenaCollisions(dt);
			ResolvePlayersInterCollisions(dt);
		}
		public World World { get; }

		void ExecCommand(EngineCommand c)
		{
			c.Execute(World);
		}
		/// <summary>
		/// Moves players according to velocity and clamps velocity to the Player.maxSpeed .
		/// </summary>
		/// <param name="dt"></param>
		void MovePlayers(double dt)
		{
			foreach (var p in World.players.Values)
			{
				float maxSpeed2 = Player.maxSpeed * Player.maxSpeed;
				if (p.Velocity.LengthSquared > maxSpeed2)
					p.Velocity = p.Velocity.Normalized() * Player.maxSpeed;
				p.Position += p.Velocity * (float)dt;
			}
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
				var cellIndex = CalcPlayersCellIndex(p, a);

				//Check all 9 sorrounding cells
				for (int yDelta = -1; yDelta <= 1; ++yDelta)
					for (int xDelta = -1; xDelta <= 1; ++xDelta)
					{
						int x = cellIndex.Item1 + xDelta;
						int y = cellIndex.Item2 + yDelta;
						//TODO when other CellTypes are intruduced
						if (x >= 0 && y >= 0 && x < a.Size && y < a.Size && a[x, y] == Arena.CellType.wall)
							ResolvePlayerCellColl(p, Arena.origin + new Vector2(x, y) * Arena.offset * Arena.boundingBox);
					}
			}
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
						var sepVec = ResolveAABBCollision(p1.Position.Xy, Player.boundingBox, p2.Position.Xy, Player.boundingBox);
						p1.Position += new Vector3(sepVec.X, sepVec.Y, 0.0f) / 2.0f;
						p2.Position -= new Vector3(sepVec.X, sepVec.Y, 0.0f) / 2.0f;
					}
				}
			}
		}
		/// <summary>
		/// Calculates (x,y) indices that represent an Arena's cell in which is the player's center located.
		/// </summary>
		static Tuple<int, int> CalcPlayersCellIndex(Player p, Arena a)
		{
			//Calculate index of the cell where the player's center is.
			var cellIndexF = (p.Position.Xy - Arena.origin) * Arena.offset + Arena.boundingBox / 2.0f;
			cellIndexF.X /= Arena.boundingBox.X;
			cellIndexF.Y /= Arena.boundingBox.Y;
			//Should not happen
			if (cellIndexF.X < 0.0f || cellIndexF.Y < 0.0f ||
				cellIndexF.X >= a.Size || cellIndexF.X >= a.Size)
				Console.WriteLine("Player is out of arena.");

			return new Tuple<int, int>((int)cellIndexF.X, (int)cellIndexF.Y);
		}
		/// <summary>
		/// Resolves AABB collision between the player and cell.
		/// </summary>
		static void ResolvePlayerCellColl(Player p, Vector2 cellCenter)
		{
			Vector2 sepVec = ResolveAABBCollision(p.Position.Xy, Player.boundingBox, cellCenter, Arena.boundingBox);
			p.Position += new Vector3(sepVec.X, sepVec.Y, 0.0f);
		}
		/// <summary>
		/// Resolves collision between two colliding AABBS, returned vector should be added to first's position to separate it from the second.
		/// Returns 0 vector if there was no collision.
		/// </summary>
		/// <returns>Separation vertor = applying it to first's position seperates it from the second AABB.
		/// Returns 0 vector if there was no collision.</returns>
		static Vector2 ResolveAABBCollision(Vector2 firstCenter, Vector2 firstDims, Vector2 secondCenter, Vector2 secondDims)
		{
			Vector2 centerDist = firstCenter - secondCenter;
			centerDist.X = Math.Abs(centerDist.X);
			centerDist.Y = Math.Abs(centerDist.Y);

			var penDepth = firstDims / 2.0f + secondDims / 2.0f - centerDist;

			//Collision
			if (penDepth.X > 0.0f && penDepth.Y > 0.0f)
			{
				//Choose the smaller correction
				//RESOLVE would choosing w.r.t velocity be better?
				if (penDepth.X < penDepth.Y)
					return new Vector2(Math.Sign(firstCenter.X - secondCenter.X) * penDepth.X, 0.0f);
				else
					return new Vector2(0.0f, Math.Sign(firstCenter.Y - secondCenter.Y) * penDepth.Y);
			}
			else//No collision
				return new Vector2();
		}
	}
}
