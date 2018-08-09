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
	class WorldRenderer
	{
		public WorldRenderer(Arena arena,IView view)
		{
			arenaRenderer = new ArenaRenderer(arena,view);
		}
		public void Render()
		{
			arenaRenderer.Render();
		}

		ArenaRenderer arenaRenderer;
	}
}
