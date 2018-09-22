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
		public Window() : base(1280, 720, GraphicsMode.Default, "Title", GameWindowFlags.FixedWindow, DisplayDevice.Default, 3, 3, GraphicsContextFlags.ForwardCompatible) { }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			Title = "Game";
			input = new Input(new Vector2(Width, Height));
			game = new Game(new GameStates.MenuState(input), input);
			RegisterInputCallbacks();
			GL.Disable(EnableCap.CullFace);

			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Texture2D);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Enable(EnableCap.Blend);
			GL.ActiveTexture(TextureUnit.Texture0);
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
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
		protected override void OnDisposed(EventArgs e)
		{
			base.OnDisposed(e);
			game.Dispose();
		}

		private void RegisterInputCallbacks()
		{
			KeyDown += (s, k) => input.SetKey(k.Key, true);
			KeyUp += (s, k) => input.SetKey(k.Key, false);
			MouseLeave += (s, m) => input.SetMousePos(Input.mouseOut);
			MouseMove += (s, m) => input.SetMousePos(new Vector2(m.X, m.Y));
			MouseDown += (s, m) => input.SetMouse(m.Button, m.IsPressed);
			MouseUp += (s, m) => input.SetMouse(m.Button, m.IsPressed);
		}

		Input input;
		Game game;
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
				Console.WriteLine(e.StackTrace);
			}
		}
	}
}
