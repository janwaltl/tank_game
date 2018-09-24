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
	class ArenaRenderer : IDisposable
	{
		public ArenaRenderer(Arena arena, IView view)
		{
			this.arena = arena;
			this.view = view;
			shader = BuildShader();

			VAO  = GL.GenVertexArray();
			GL.BindVertexArray(VAO);
			qVBO = QuadGenerator.GenQuadVBO(0, 1);
			qIBO = QuadGenerator.GenQuadIBO();
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
					var pos = Arena.origin + Arena.offset * (new Vector2(x, y) * Arena.boundingBox);
					data[2 * i] = new Vector3(pos.X, pos.Y, 0.0f);
					data[2 * i + 1] = GetCellColor(arena[x, y]);//Color
				}
			int VBO = GL.GenBuffer();
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
				case Arena.CellType.spawn:
					return new Vector3(0.8f, 0.8f, 0.8f);
				case Arena.CellType.shield:
					return new Vector3(0.4f, 0.8f, 0.4f);
				default:
					throw new NotImplementedException("Someone forgot to add case for this CellType");
			}
		}

		public void Dispose()
		{
			((IDisposable)shader).Dispose();
			GL.DeleteBuffer(iVBO);
			GL.DeleteBuffer(qVBO);
			GL.DeleteBuffer(qIBO);
			GL.DeleteVertexArray(VAO);
			GL.DeleteTexture(texID);
		}

		readonly IView view;
		readonly Arena arena;
		readonly int iVBO, qVBO, qIBO, VAO;
		readonly int texID;
		ShaderProgram shader;
	}
}
