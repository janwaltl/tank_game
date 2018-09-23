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
	class Renderer : IDisposable
	{
		public Renderer(Input input, Engine.Engine e, int playerID)
		{
			this.input = input;
			engine = e;
			pID = playerID;
			cam = new Camera(input.Viewport(), new Vector3(0.0f, 0.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f));
			float x = 8.0f;
			float y = x / cam.AspectRatio;
			cam.Proj = Matrix4.CreateOrthographicOffCenter(-x, x, -y, y, -10.0f, 10.0f);
			//cam.Proj = Matrix4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.DegreesToRadians(90), cam.AspectRatio, 0.01f, 10.0f);
			fontManager = new FontManager(cam);
			worldRenderer = new WorldRenderer(e.World, cam,fontManager);
			tableRenderer = new TableRenderer();
		}
		public void Render(double dt)
		{
			var players = engine.World.players;
			if (players.ContainsKey(pID))//Player is present (=not dead, not fully connected yet)
			{
				var camPos = players[pID].Position;
				cam.Look(camPos + new Vector3(0.0f, 0.0f, 5.0f), new Vector3(0.0f, 0.0f, -1.0f), Camera.defaultUp);
			}
			worldRenderer.Render();

			if (input.IsKeyPressed(OpenTK.Input.Key.Tab))
				tableRenderer.Draw(engine.World.players.Values, fontManager);
		}

		public void Dispose()
		{
			((IDisposable)worldRenderer).Dispose();
		}
		public ITextRenderer TextRenderer { get { return fontManager; } }
		int pID;
		Camera cam;
		WorldRenderer worldRenderer;
		TableRenderer tableRenderer;
		FontManager fontManager;
		Input input;
		Engine.Engine engine;
	}
}
