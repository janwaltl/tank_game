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
	class PlayerRenderer : IDisposable
	{
		public PlayerRenderer(IView view, ITextRenderer textRenderer)
		{
			this.textRenderer = textRenderer;
			this.view = view;
			GenBuffers();
			BuildShader();
			InitHealthBar();
		}

		private void InitHealthBar()
		{
			healthBar.VAO = GL.GenVertexArray();
			GL.BindVertexArray(healthBar.VAO);
			healthBar.VBO = QuadGenerator.GenQuadVBO(0, 1);
			healthBar.IBO = QuadGenerator.GenQuadIBO();
			healthBar.numIndices = 4;
			GL.BindVertexArray(0);

			string vSource, fSource;
			using (var f = new StreamReader("Res/quad.vert")) vSource = f.ReadToEnd();
			using (var f = new StreamReader("Res/quad.frag")) fSource = f.ReadToEnd();
			healthBar.shader = new ShaderProgram(vSource, fSource);
		}

		public void RenderPlayer(Engine.Player p)
		{
			shader.Bind();
			shader.SetUniform("proj", view.Proj);
			shader.SetUniform("view", view.View);
			var trans = Matrix4.CreateTranslation(p.Position);
			//Rotate the tank based on its current moving direction
			shader.SetUniform("model", Matrix4.CreateRotationZ(p.TankAngle) * trans);
			RenderTank(tank, p.Color);
			shader.SetUniform("model", Matrix4.CreateRotationZ(p.TowerAngle) * trans);
			RenderTank(tower, 0.5f * p.Color);
			RenderHealthBars(p);
			textRenderer.DrawInWorld($"Player{p.ID}", p.Position + new Vector3(-Engine.Player.radius -0.2f, Engine.Player.radius, 0.5f),
				new Vector3(1.0f, 1.0f, 1.0f), .3f);
			shader.UnBind();
		}
		/// <summary>
		/// Render two-colored mesh. Used for tank and tankTower
		/// </summary>
		/// <param name="data"></param>
		private void RenderTank(RenderData data, Vector3 pColor)
		{
			GL.BindVertexArray(data.VAO);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, data.IBO);
			shader.SetUniform("playerCol", pColor);
			GL.DrawElements(PrimitiveType.Triangles, data.materials[0].count,
							DrawElementsType.UnsignedInt, data.materials[0].startIndex * 4);//4==sizeof(int)
			shader.SetUniform("playerCol", new Vector3(0.2f, 0.2f, 0.2f));
			GL.DrawElements(PrimitiveType.Triangles, data.materials[1].count,
							DrawElementsType.UnsignedInt, data.materials[1].startIndex * 4);//4==sizeof(int)
			GL.BindVertexArray(0);
		}
		private void RenderHealthBars(Engine.Player p)
		{
			GL.BindVertexArray(healthBar.VAO);
			healthBar.shader.Bind();
			shader.SetUniform("proj", view.Proj);
			shader.SetUniform("view", view.View);
			var scale = p.CurrHealth / (float)Engine.Player.initHealth;
			var offset = -Engine.Player.radius - 0.1f;
			var color = new Vector3(1.0f, 0.0f, 0.0f);
			RenderQuad(p, scale, offset, color);

			scale = p.CurrShields / (float)Engine.Player.initShields;
			offset = -Engine.Player.radius;
			color = new Vector3(0.3f, 0.3f, 1.0f);
			RenderQuad(p, scale, offset, color);

			healthBar.shader.UnBind();
			GL.BindVertexArray(0);
		}

		private void RenderQuad(Engine.Player p, float scale, float offset, Vector3 color)
		{
			var modelMat = Matrix4.CreateTranslation(p.Position + new Vector3(0.0f, offset, 0.1f));
			modelMat = Matrix4.CreateScale(scale, 0.1f, 1.0f) * modelMat;
			shader.SetUniform("model", modelMat);
			healthBar.shader.SetUniform("col", color);
			GL.DrawArrays(PrimitiveType.TriangleFan, 0, healthBar.numIndices);
		}

		private struct RenderData
		{
			public struct Material
			{
				public int startIndex;
				public int count;
				public string name;
			}
			public int VAO, VBO, IBO;
			public int numIndices;
			public List<Material> materials;
		}
		private struct HealthBar
		{
			public int VAO, VBO, IBO;
			public ShaderProgram shader;
			public int numIndices;
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
			var models = ObjModel.FromFile("Res/Tank.obj");
			if (models.Count != 2 ||
				models[0].name != "Tank" ||
				models[1].name != "TankTower" ||
				models[0].materials.Count != 2 ||
				models[0].materials[0].name != "Colored" ||
				models[0].materials[1].name != "Black" ||
				models[1].materials[0].name != "Colored" ||
				models[1].materials[1].name != "Black")
				throw new Exception("Corrupted Tank model file.");
			tank = GenBuffersFromModel(models[0], 0);

			tower = GenBuffersFromModel(models[1], 0);
		}

		private RenderData GenBuffersFromModel(ObjModel model, int posAttribLoc)
		{
			int VAO = GL.GenVertexArray();
			int VBO = GL.GenBuffer();
			int IBO = GL.GenBuffer();
			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(model.vertices.Count * Vector3.SizeInBytes), model.vertices.ToArray(), BufferUsageHint.StaticDraw);
			GL.VertexAttribPointer(posAttribLoc, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, posAttribLoc);//Position
			GL.EnableVertexAttribArray(posAttribLoc);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			var numIndices = model.faces.Count * 3;
			var indices = new int[numIndices];
			for (int i = 0; i < model.faces.Count; i++)
			{
				indices[3 * i + 0] = model.faces[i].v0;
				indices[3 * i + 1] = model.faces[i].v1;
				indices[3 * i + 2] = model.faces[i].v2;
			}

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
			GL.BindVertexArray(0);
			var mats = new List<RenderData.Material>();
			foreach (var m in model.materials)
			{
				mats.Add(new RenderData.Material { startIndex = m.first * 3, count = (m.last - m.first + 1) * 3, name = m.name });
			}
			return new RenderData { VAO = VAO, VBO = VBO, IBO = IBO, numIndices = numIndices, materials = mats };
		}

		public void Dispose()
		{
			((IDisposable)shader).Dispose();
			GL.DeleteBuffer(healthBar.VBO);
			GL.DeleteBuffer(healthBar.IBO);
			GL.DeleteBuffer(tank.IBO);
			GL.DeleteBuffer(tank.VBO);
			GL.DeleteBuffer(tower.IBO);
			GL.DeleteBuffer(tower.VBO);

			GL.DeleteVertexArray(healthBar.VAO);
			GL.DeleteVertexArray(tower.VAO);
			GL.DeleteVertexArray(tank.VAO);
		}

		RenderData tank, tower;
		HealthBar healthBar;
		ShaderProgram shader;
		IView view;
		ITextRenderer textRenderer;
	}
}
