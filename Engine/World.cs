using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	public class World
	{
		public World(Arena arena)
		{
			Arena = arena;
			players = new Dictionary<int, Player>();
		}
		public Arena Arena { get; }
		/// <summary>
		/// List of players indexed by their playerID.
		/// </summary>
		public Dictionary<int, Player> players;
		//TODO other stuff - projectiles, buffs
	}
}
