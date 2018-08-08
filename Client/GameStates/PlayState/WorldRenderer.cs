using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Client.Graphics;
namespace Client.Playing
{
	class WorldRenderer
	{
		public WorldRenderer()
		{
			using (var vReader = new StreamReader("Res/test.vert"))
			{
				using (var fReader = new StreamReader("Res/test.frag"))
				{
					var vSource = vReader.ReadToEnd();
					var fSource = fReader.ReadToEnd();
					shader = new ShaderProgram(vSource, fSource);

					GL.CreateBuffers(1, out VBO);
					GL.CreateBuffers(1, out IBO);
					GL.CreateVertexArrays(1, out VAO);

					var vboData = new Vector3[] {
						new Vector3( 0.5f, 0.5f, 0.0f),new Vector3( 0.5f, 0.5f, 0.0f),
						new Vector3( 0.5f,-0.5f, 0.0f),new Vector3( 0.5f, 0.5f, 0.0f),
						new Vector3(-0.5f,-0.5f, 0.0f),new Vector3( 0.5f, 0.5f, 0.0f),
						new Vector3(-0.5f, 0.5f, 0.0f),new Vector3( 0.5f, 0.5f, 0.0f), };
					GL.BindVertexArray(VAO);
					GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
					GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vboData.Length * Vector3.SizeInBytes), vboData, BufferUsageHint.StaticDraw);
					GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes * 2, 0);//Position
					GL.EnableVertexAttribArray(0);
					GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes * 2, Vector3.SizeInBytes);//Color
					GL.EnableVertexAttribArray(1);

					var indices = new uint[] { 0, 1, 2, 3 };
					GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
					GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
					GL.BindVertexArray(0);
				}
			}
		}
		public void Render()
		{
			shader.Bind();
			shader.SetUniform("proj", Matrix4.Identity);
			shader.SetUniform("view", Matrix4.Identity);
			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.DrawElements(BeginMode.TriangleFan, 4, DrawElementsType.UnsignedInt, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindVertexArray(0);
			shader.UnBind();
		}

		ShaderProgram shader;
		int VBO, VAO, IBO;
	}
}
