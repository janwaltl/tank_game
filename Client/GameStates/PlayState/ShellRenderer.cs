using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Engine;
namespace Client.Playing
{
	/// <summary>
	/// Class that renderes fired shells.
	/// </summary>
	class ShellRenderer:IDisposable
	{
		public ShellRenderer(IView view)
		{
			this.view = view;
			GenBuffers(0, 1);
			BuildShader();
		}
		public void RenderShells(List<TankShell> shells)
		{
			UpdateBuffer(shells);

			GL.BindVertexArray(VAO);
			shader.Bind();
			shader.SetUniform("proj", view.Proj);
			shader.SetUniform("view", view.View);
			GL.DrawElementsInstanced(PrimitiveType.TriangleFan, 4, DrawElementsType.UnsignedInt, (IntPtr)0, shells.Count);
			GL.BindVertexArray(0);
		}

		private void UpdateBuffer(List<TankShell> shells)
		{
			int buffLength = shells.Count * Vector2.SizeInBytes;
			GL.BindBuffer(BufferTarget.ArrayBuffer, sVBO);
			GL.BufferData(BufferTarget.ArrayBuffer, buffLength, (IntPtr)0, BufferUsageHint.StreamDraw);
			unsafe
			{
				float* ptr = (float*)GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly).ToPointer();
				foreach (var s in shells)
				{
					*(ptr++) = s.position.X;
					*(ptr++) = s.position.Y;
				}
				GL.UnmapBuffer(BufferTarget.ArrayBuffer);
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		void BuildShader()
		{
			string vSource, fSource;
			using (var f = new StreamReader("Res/shell.vert")) vSource = f.ReadToEnd();
			using (var f = new StreamReader("Res/shell.frag")) fSource = f.ReadToEnd();
			shader = new ShaderProgram(vSource, fSource);
		}
		void GenBuffers(int quadPosAttribLoc, int shellPosAttribLoc)
		{
			VAO = GL.GenVertexArray();
			qVBO = GL.GenBuffer();
			sVBO = GL.GenBuffer();
			IBO = GL.GenBuffer();

			var quad = new Vector3[]
			{
				new Vector3( TankShell.boundingBox.X/2.0f, TankShell.boundingBox.Y/2.0f, 0.0f),
				new Vector3( TankShell.boundingBox.X/2.0f,-TankShell.boundingBox.Y/2.0f, 0.0f),
				new Vector3(-TankShell.boundingBox.X/2.0f,-TankShell.boundingBox.Y/2.0f, 0.0f),
				new Vector3(-TankShell.boundingBox.X/2.0f, TankShell.boundingBox.Y/2.0f, 0.0f)
			};
			var indices = new uint[] { 0, 1, 2, 3 };

			GL.BindVertexArray(VAO);

			GL.BindBuffer(BufferTarget.ArrayBuffer, qVBO);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(quad.Length * Vector3.SizeInBytes), quad, BufferUsageHint.StaticDraw);
			GL.VertexAttribPointer(quadPosAttribLoc, 3, VertexAttribPointerType.Float, false, 0, 0);//quad position
			GL.EnableVertexAttribArray(quadPosAttribLoc);

			GL.BindBuffer(BufferTarget.ArrayBuffer, sVBO);
			GL.VertexAttribPointer(shellPosAttribLoc, 2, VertexAttribPointerType.Float, false, 0, 0);//shells' positions
			GL.EnableVertexAttribArray(shellPosAttribLoc);
			GL.VertexAttribDivisor(shellPosAttribLoc, 1);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * 4), indices, BufferUsageHint.StaticDraw);

			GL.BindVertexArray(0);
		}

		public void Dispose()
		{
			((IDisposable)shader).Dispose();
			GL.DeleteBuffer(qVBO);
			GL.DeleteBuffer(sVBO);
			GL.DeleteBuffer(IBO);
			GL.DeleteVertexArray(VAO);
		}

		int VAO, qVBO, sVBO, IBO;
		ShaderProgram shader;
		IView view;
	}
}
