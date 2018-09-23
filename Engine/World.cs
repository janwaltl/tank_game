using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public class World
	{
		public World(Arena arena)
		{
			Arena = arena;
			players = new Dictionary<int, Player>();
			shells = new List<TankShell>();
			shieldPickups = new Dictionary<int, ShieldPickup>();
		}
		public Arena Arena { get; }
		/// <summary>
		/// List of players indexed by their playerID.
		/// </summary>
		public Dictionary<int, Player> players;
		public List<TankShell> shells;
		/// <summary>
		/// List of active shield pickups in the arena, key=ID.
		/// </summary>
		public Dictionary<int,ShieldPickup> shieldPickups;
	}
}
