using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine;
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
		public Renderer(Input input, Engine.Engine e)
		{
			cam = new Camera(input.Viewport(), new Vector3(0.0f, 0.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f));
			cam.Proj = Matrix4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.DegreesToRadians(90), cam.AspectRatio, 0.01f, 10.0f);
			engine = e;
			worldRenderer = new WorldRenderer(e.World, cam);
		}
		public void Render(double dt)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			worldRenderer.Render();
		}
		Camera cam;
		WorldRenderer worldRenderer;
		Engine.Engine engine;
	}
}
