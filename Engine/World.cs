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
		//List of players indexed by their playerID.
		public readonly Dictionary<int, Player> players;
		//TODO other stuff - projectiles, buffs
	}
}
