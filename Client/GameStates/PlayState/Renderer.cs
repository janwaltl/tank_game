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
		public Renderer(Input input, Engine.Engine e, int playerID)
		{
			engine = e;
			pID = playerID;
			cam = new Camera(input.Viewport(), new Vector3(0.0f, 0.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f));
			cam.Proj = Matrix4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.DegreesToRadians(90), cam.AspectRatio, 0.01f, 10.0f);
			worldRenderer = new WorldRenderer(e.World, cam);
		}
		public void Render(double dt)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			var players = engine.World.players;
			if (players.ContainsKey(pID))//Player is present (=not dead, not fully connected yet)
			{
				var camPos = players[pID].Position;
				cam.Look(camPos + new Vector3(0.0f, 0.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f), Camera.defaultUp);
			}
			worldRenderer.Render();
		}
		int pID;
		Camera cam;
		WorldRenderer worldRenderer;
		Engine.Engine engine;
	}
}
