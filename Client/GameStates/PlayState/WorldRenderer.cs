using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Client.Graphics;
using Engine;

namespace Client.Playing
{
	class WorldRenderer: IDisposable
	{
		public WorldRenderer(World world, IView view)
		{
			this.world = world;
			arenaRenderer = new ArenaRenderer(world.Arena, view);
			playerRenderer = new PlayerRenderer(view);
			shellRenderer = new ShellRenderer(view);
		}
		public void Render()
		{
			arenaRenderer.Render();
			foreach (var pair in world.players)
			{
				playerRenderer.RenderPlayer(pair.Value);
			}
			shellRenderer.RenderShells(world.shells);
		}

		public void Dispose()
		{
			((IDisposable)arenaRenderer).Dispose();
			((IDisposable)playerRenderer).Dispose();
			((IDisposable)shellRenderer).Dispose();
		}

		ArenaRenderer arenaRenderer;
		PlayerRenderer playerRenderer;
		ShellRenderer shellRenderer;
		readonly World world;
	}
}
