using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Client
{
	class Game : OpenTK.GameWindow
	{
		public Game() : base(640, 640) { }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Title = "Game";
			GL.ClearColor(Color4.DarkOrange);

			s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			s.Connect(IPAddress.Parse("127.0.0.1"), 32653); // 2
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			SwapBuffers();
		}
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);
			Console.Write("Zadej nejakej text : ");

			string q = Console.ReadLine();                 // 3
			byte[] data = Encoding.BigEndianUnicode.GetBytes(q);    // 3
			s.Send(data);
		}
		Socket s;

		static void Main(string[] args)
		{
			try
			{
				Game g = new Game();
				g.Run(60.0, 60.0);
			}
			catch (Exception e) // 1
			{
				Console.WriteLine("Error: \n" + e.Message);
			}
		}
	}
}
