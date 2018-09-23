﻿using System;
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
	class WorldRenderer : IDisposable
	{
		public WorldRenderer(World world, IView view, ITextRenderer textRenderer)
		{
			this.world = world;
			arenaRenderer = new ArenaRenderer(world.Arena, view);
			playerRenderer = new PlayerRenderer(view, textRenderer);
			shellRenderer = new ShellRenderer(view);
			shieldRenderer = new ShieldPickupRenderer();
			this.textRenderer = textRenderer;
		}
		public void Render()
		{
			arenaRenderer.Render();
			shellRenderer.RenderShells(world.shells);
			foreach (var pair in world.players)
			{
				playerRenderer.RenderPlayer(pair.Value);
			}
			shieldRenderer.Render(world.shieldPickups.Values, textRenderer);
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
		ShieldPickupRenderer shieldRenderer;
		ITextRenderer textRenderer;
		readonly World world;
	}
}
