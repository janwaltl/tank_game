using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Client.Graphics;
using Engine;
using OpenTK.Graphics.OpenGL;
using OpenTK;
namespace Client.Playing
{
	class ArenaRenderer
	{
		public ArenaRenderer(Arena arena, IView view)
		{
			this.arena = arena;
			this.view = view;
			shader = BuildShader();

			GL.CreateVertexArrays(1, out VAO);
			GL.BindVertexArray(VAO);
			qVBO = GenQuadVBO(0, 1);
			qIBO = GenQuadIBO();
			//Keep IBO bounded to VAO
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, qIBO);
			iVBO = GenInstancedDataVBO(arena, 2, 3);
			GL.BindVertexArray(0);
			texID = GenTexture();
		}

		public void Render()
		{
			shader.Bind();
			shader.SetUniform("proj", view.Proj);
			shader.SetUniform("view", view.View);
			shader.SetUniformSampler2D("tex", 0);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, texID);
			GL.BindVertexArray(VAO);
			GL.DrawElementsInstanced(PrimitiveType.TriangleFan, 4, DrawElementsType.UnsignedInt, (IntPtr)0, arena.Size * arena.Size);
			GL.BindVertexArray(0);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			shader.UnBind();
		}
		/// <summary>
		/// Loads texture for the arena's tiles.
		/// </summary>
		/// <returns></returns>
		static int GenTexture()
		{
			GL.Enable(EnableCap.Texture2D);
			int texID = GL.GenTexture();
			var texture = new Bitmap("Res/floor.jpg");
			GL.BindTexture(TextureTarget.Texture2D, texID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			var bitmap = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height),
											System.Drawing.Imaging.ImageLockMode.ReadOnly,
											System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texture.Width, texture.Height,
						  0, PixelFormat.Rgba, PixelType.UnsignedByte, bitmap.Scan0);
			texture.UnlockBits(bitmap);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			return texID;
		}
		static ShaderProgram BuildShader()
		{
			string vSource, fSource;
			using (var f = new StreamReader("Res/arena.vert")) vSource = f.ReadToEnd();
			using (var f = new StreamReader("Res/arena.frag")) fSource = f.ReadToEnd();
			return new ShaderProgram(vSource, fSource);
		}
		/// <summary>
		/// Generates VBO for an unit quad vec3 points and sets the position,uv as the attributes at given locations.
		/// </summary>
		/// <param name="posAttribLoc">To which attribute location bind the position of the quad.</param>
		/// <param name="posAttribLoc">To which attribute location bind the UF coordinates of the quad.</param>
		/// <returns>VBO containing the quad</returns>
		static int GenQuadVBO(int posAttribLoc, int uvAttribLoc)
		{
			var quad = new Vector3[]
			{
				new Vector3( 0.5f, 0.5f, 0.0f),
				new Vector3( 0.5f,-0.5f, 0.0f),
				new Vector3(-0.5f,-0.5f, 0.0f),
				new Vector3(-0.5f, 0.5f, 0.0f)
			};
			var uv = new Vector2[]
			{
				new Vector2(1.0f,1.0f),
				new Vector2(1.0f,0.0f),
				new Vector2(0.0f,0.0f),
				new Vector2(0.0f,1.0f),
			};
			GL.CreateBuffers(1, out int VBO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			int quadLen = quad.Length * Vector3.SizeInBytes;
			int uvLen = uv.Length * Vector2.SizeInBytes;
			int buffLen = quadLen + uvLen;
			GL.BufferData(BufferTarget.ArrayBuffer, buffLen, (IntPtr)0, BufferUsageHint.StaticDraw);
			GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, (IntPtr)quadLen, quad);
			GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)quadLen, (IntPtr)uvLen, uv);
			GL.VertexAttribPointer(posAttribLoc, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);//Position
			GL.VertexAttribPointer(uvAttribLoc, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, quadLen);//Position
			GL.EnableVertexAttribArray(posAttribLoc);
			GL.EnableVertexAttribArray(uvAttribLoc);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			return VBO;
		}
		/// <summary>
		/// Generates IBO for the quad to be rendered with GL_TRIANGLE_FAN.
		/// </summary>
		/// <returns>IBO containing the indices.</returns>
		static int GenQuadIBO()
		{
			GL.CreateBuffers(1, out int IBO);
			var indices = new uint[] { 0, 1, 2, 3 };
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
			return IBO;
		}
		/// <summary>
		/// Creates VBO buffer and fills it with n*n pairs of (vec3 pos, vec3 col) corresponding to cell of the arena.
		/// pos is position of the cell(=quad)'s center.Binds those data to passed attributes.
		/// </summary>
		/// <param name="posAttribLoc">Where to bind vec3 positions</param>
		/// <param name="colAttribLoc">Where to bind vec3 colors</param>
		/// <returns>VBO containing the data.</returns>
		static int GenInstancedDataVBO(Arena arena, int posAttribLoc, int colAttribLoc)
		{
			int s = arena.Size;
			var data = new Vector3[s * s * 2];//pos+color
			for (int y = 0; y < s; y++)
				for (int x = 0; x < s; x++)
				{
					int i = y * s + x;
					data[2 * i] = new Vector3(x, y, 0.0f);//Position
					data[2 * i + 1] = GetCellColor(arena[x, y]);//Color
				}
			GL.CreateBuffers(1, out int VBO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			int vecSize = Vector3.SizeInBytes;
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(data.Length * vecSize), data, BufferUsageHint.StaticDraw);
			GL.VertexAttribPointer(posAttribLoc, 3, VertexAttribPointerType.Float, false, 2 * vecSize, 0);//Position
			GL.VertexAttribPointer(colAttribLoc, 3, VertexAttribPointerType.Float, false, 2 * vecSize, vecSize);//Color
			GL.EnableVertexAttribArray(posAttribLoc);
			GL.EnableVertexAttribArray(colAttribLoc);
			GL.VertexAttribDivisor(posAttribLoc, 1);
			GL.VertexAttribDivisor(colAttribLoc, 1);
			return VBO;
		}
		/// <summary>
		/// Returns color based of the cell based on its type.
		/// </summary>
		static Vector3 GetCellColor(Arena.CellType type)
		{
			switch (type)
			{
				case Arena.CellType.empty:
					return new Vector3(1.0f, 1.0f, 1.0f);
				case Arena.CellType.wall:
					return new Vector3(0.4f, 0.4f, 0.4f);
				default:
					throw new NotImplementedException("Someone forgot to add case for this CellType");
			}
		}
		readonly IView view;
		readonly Arena arena;
		readonly int iVBO, qVBO, qIBO, VAO;
		readonly int texID;
		ShaderProgram shader;
	}
}
