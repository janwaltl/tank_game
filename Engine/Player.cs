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
		public Player(int playerID, Vector3 pos, Vector3 col)
		{
			ID = playerID;
			Position = pos;
			Color = col;
		}
		public readonly int ID;
		public Vector3 Position { get; set; }
		//TODO Probably replace with some model or something...
		public Vector3 Color { get; set; }
		/// <summary>
		/// Bounding box of the player for the collision.
		/// Centered quad with X,Y dimensions.
		/// </summary>
		public readonly static Vector2 boundingBox = new Vector2(1.0f, 1.0f);
	}
}
