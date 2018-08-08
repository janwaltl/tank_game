using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Client
{
	class Window : OpenTK.GameWindow
	{
		public Window() : base(640, 640) { }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			Title = "Game";
			GL.ClearColor(Color4.DarkOrange);

			game = new Game(new GameStates.MenuState());
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			double dt = e.Time;

			game.Render(dt);
			SwapBuffers();
		}
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			double dt = e.Time;

			game.Update(dt);
		}

		Game game;
		protected override void OnDisposed(EventArgs e)
		{
			base.OnDisposed(e);
			game.Dispose();
		}
		static void Main(string[] args)
		{
			try
			{
				using (Window gWin = new Window())
				{
					gWin.Run(60.0, 60.0);
				}
			}
			catch (Exception e) // 1
			{
				Console.WriteLine("Error: \n" + e.Message);
			}
		}
	}
}
