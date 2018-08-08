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
	class WorldRenderer
	{
		public void Render()
		{
			GL.Begin(PrimitiveType.Triangles);
			GL.Color3(1.0, 0.0, 0.0);
			GL.Vertex3(0.0, 0.5, 0.0);
			GL.Color3(0.0, 1.0, 0.0);
			GL.Vertex3(-0.5, 0.0, 0.0);
			GL.Color3(0.0, 0.0, 1.0);
			GL.Vertex3(+0.5, 0.0, 0.0);
			GL.End();
		}
	}
}
