using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	public abstract class Command
	{
		public abstract void Execute(World p);
	}
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

		public void Update(double dt, IEnumerable<Command> commands)
		{
			foreach (var c in commands)
				ExecCommand(c);
		}

		public World World { get; }

		private void ExecCommand(Command c)
		{
			c.Execute(World);
		}
	}
}
