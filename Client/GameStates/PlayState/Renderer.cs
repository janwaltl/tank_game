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
		public Renderer(Input input)
		{
			cam = new Camera(input.Viewport(), new Vector3(5.0f, 5.0f, 0.0f), new Vector3(5.0f, 5.0f, -1.0f));
			//TEMP Replace with engine
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
