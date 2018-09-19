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
		/// The radius of the bounding sphere of the player for the collision.
		/// </summary>
		public readonly static float radius = 0.5f;
		/// <summary>
		/// Player's movemevent speed in units per second
		/// </summary>
		public readonly static float speed = 2.0f;
		/// <summary>
		/// Time between two fired shells. In seconds.
		/// </summary>
		public readonly static double fireCooldown = 1.0;
		public Player(int playerID, Vector3 pos, Vector3 col)
		{
			ID = playerID;
			Position = pos;
			Color = col;
			CurrFireCooldown = 0.0;
		}
		public readonly int ID;
		public Vector3 Position { get; set; }
		public Vector3 Color { get; set; }
		/// <summary>
		/// In radians, 0=down, PI/2=right
		/// </summary>
		public float TowerAngle { get; set; }
		/// <summary>
		/// In radians, 0=down, PI/2=right
		/// </summary>
		public float TankAngle { get; set; }
		/// <summary>
		/// Current cooldown of the fire action. 
		/// negative value means the action is ready, positive value represents
		/// number of seconds remaining on the cooldown.
		/// NOT in [0,fireCooldown] range.
		/// </summary>
		public double CurrFireCooldown { get; set; }
	}
}
