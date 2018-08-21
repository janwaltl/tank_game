using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public class Player
	{
		/// <summary>
		/// Bounding box of the player for the collision.
		/// Centered quad with X,Y dimensions.
		/// </summary>
		public readonly static Vector2 boundingBox = new Vector2(1.0f, 1.0f);
		/// <summary>
		/// Maximum player's speed in units per second
		/// </summary>
		public readonly static float maxSpeed = 2.0f;
		/// <summary>
		/// How quickly can player change its velocity in units per second^2
		/// </summary>
		public readonly static float acceleration = 5.0f;

		public Player(int playerID, Vector3 pos, Vector3 vel, Vector3 col)
		{
			ID = playerID;
			Position = pos;
			Color = col;
		}
		public readonly int ID;
		public Vector3 Position { get; set; }
		public Vector3 Velocity { get; set; }
		public Vector3 Color { get; set; }
		public float TowerAngle { get; set; }
	}
}
