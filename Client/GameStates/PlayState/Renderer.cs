using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.Graphics;
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
		/// <summary>
		/// Creates renderer with desired canvas resolution.
		/// </summary>
		public Renderer(int x, int y)
		{
			//TEMP Replace with engine's
			cam = new Camera(new Vector2(x, y), new Vector3(5.0f, 5.0f, 0.0f), new Vector3(5.0f, 5.0f, -1.0f));
			worldRenderer = new WorldRenderer(new Engine.Arena(10), cam);

		}
		public void Render(double dt)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			worldRenderer.Render();
		}
		Camera cam;
		WorldRenderer worldRenderer;
	}
}
