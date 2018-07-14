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

			var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23545);
			s = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			s.Connect(endPoint);
			writer = new StreamWriter(new NetworkStream(s));

			engine = new Engine.Engine(new Engine.World());
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
			double dt = e.Time;

			UploadInputs(dt);

			//Recieve server commands and pass them to the engine for execution
			engine.Update(dt, new List<Engine.Command>());
		}
		private void UploadInputs(double dt)
		{
			Console.Write("Zadej nejakej text : ");
			string q = Console.ReadLine();
			byte[] data = Encoding.BigEndianUnicode.GetBytes(q);
			writer.WriteLine(q);
			writer.Flush();
		}
		private Socket s;
		private StreamWriter writer;
		private Engine.Engine engine;

		static void Main(string[] args)
		{
			try
			{
				Window g = new Window();
				g.Run(60.0, 60.0);
			}
			catch (Exception e) // 1
			{
				Console.WriteLine("Error: \n" + e.Message);
			}
		}
	}
}
