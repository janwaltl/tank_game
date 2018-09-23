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
		/// <summary>
		/// Player's initial amount of health.
		/// </summary>
		public readonly static byte initHealth = 100;
		/// <summary>
		/// Player's initial amount of shields.
		/// </summary>
		public readonly static byte initShields = 100;

		/// <summary>
		/// Amount of shields regenerated per second.
		/// </summary>
		public readonly static byte shieldRegen = 5;
		public Player(int playerID, Vector3 pos, Vector3 col, int kills, int deaths)
		{
			ID = playerID;
			Position = pos;
			Color = col;
			CurrFireCooldown = 0.0;
			CurrHealth = initHealth;
			CurrShields = initShields;
			KillCount = kills;
			DeathCount = deaths;
		}
		public readonly int ID;
		public int KillCount { get; set; }
		public int DeathCount { get; set; }
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
		/// <summary>
		/// Health of the player, if it reaches zero the player's tank is destroyed.
		/// </summary>
		public byte CurrHealth { get; set; }
		/// <summary>
		/// Shiled of the player are the first thing that will tak the damage of a shell.
		/// They are constantly regenerating if they are above zero.
		/// </summary>
		public byte CurrShields { get; set; }
	}
}
