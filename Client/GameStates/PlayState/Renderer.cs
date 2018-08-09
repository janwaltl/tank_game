using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace Client.Playing
{
	/// <summary>
	/// Renders playing state = the game.
	/// </summary>
	class Renderer
	{
		public Renderer()
		{
			//TEMP Replace with engine's
			worldRenderer = new WorldRenderer(new Engine.Arena(10));
		}
		public void Render(double dt)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			worldRenderer.Render();
		}
		WorldRenderer worldRenderer;
	}
}
