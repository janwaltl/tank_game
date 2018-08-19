using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Client.Playing
{
	/// <summary>
	/// Provides means to render a Player.
	/// </summary>
	class PlayerRenderer
	{
		public PlayerRenderer(IView view)
		{
			this.view = view;
			GenBuffers();
			BuildShader();
		}
		public void RenderPlayer(Engine.Player p)
		{
			shader.Bind();
			shader.SetUniform("proj", view.Proj);
			shader.SetUniform("view", view.View);
			shader.SetUniform("model", Matrix4.CreateTranslation(p.Position));
			shader.SetUniform("playerCol", p.Color);

			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.DrawElements(PrimitiveType.TriangleFan, 4, DrawElementsType.UnsignedInt, 0);
			GL.BindVertexArray(0);
			shader.UnBind();
		}
		private void BuildShader()
		{
			string vSource, fSource;
			using (var f = new StreamReader("Res/player.vert")) vSource = f.ReadToEnd();
			using (var f = new StreamReader("Res/player.frag")) fSource = f.ReadToEnd();
			shader = new ShaderProgram(vSource, fSource);
		}
		private void GenBuffers()
		{
			var quadHalf = Engine.Player.boundingBox / 2.0f;
			var quad = new Vector3[] {
						new Vector3( quadHalf.X, quadHalf.Y, 0.0f),
						new Vector3( quadHalf.X,-quadHalf.Y, 0.0f),
						new Vector3(-quadHalf.X,-quadHalf.Y, 0.0f),
						new Vector3(-quadHalf.X, quadHalf.Y, 0.0f)};
			var indices = new uint[] { 0, 1, 2, 3 };
			GL.CreateVertexArrays(1, out VAO);
			GL.CreateBuffers(1, out VBO);
			GL.CreateBuffers(1, out IBO);
			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(quad.Length * Vector3.SizeInBytes), quad, BufferUsageHint.StaticDraw);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);//Position
			GL.EnableVertexAttribArray(0);
			//GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
			GL.BindVertexArray(0);
			//GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
		int VAO, VBO, IBO;
		ShaderProgram shader;
		IView view;
	}
}
