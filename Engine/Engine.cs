using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public void RunPhysics(double dt)
		{
			//TODO Run physics
		}
		public World World { get; }

		private void ExecCommand(EngineCommand c)
		{
			c.Execute(World);
		}
	}
}
