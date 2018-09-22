using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Client.Graphics
{
	class FontManager : ITextRenderer
	{
		public FontManager(IView worldView)
		{
			wView = worldView;
			fontAtlasTexID = GenFontAtlas();
			VAO = GL.GenVertexArray();
			VBO = GL.GenBuffer();
			IBO = GL.GenBuffer();
			GL.BindVertexArray(VAO);
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			int posAttribLoc = 0, uvAttribLoc = 1;
			int stride = Vector2.SizeInBytes + Vector3.SizeInBytes;
			GL.VertexAttribPointer(posAttribLoc, 3, VertexAttribPointerType.Float, false, stride, 0);//Position
			GL.VertexAttribPointer(uvAttribLoc, 2, VertexAttribPointerType.Float, false, stride, Vector3.SizeInBytes);//UV
			GL.EnableVertexAttribArray(posAttribLoc);
			GL.EnableVertexAttribArray(uvAttribLoc);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindVertexArray(0);

			shader = BuildShader();
			shader.Bind();
			shader.SetUniformSampler2D("tex", 0);
			shader.UnBind();
		}
		/// <summary>
		/// Draws the text in world coordinates.
		/// </summary>
		/// <param name="text">Text to draw.</param>
		/// <param name="pos">World coordinates of the first character.(botom left corner)</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="glyphSize">Text height in world coordinates.</param>
		public void DrawInWorld(string text, Vector3 pos, Vector3 color, float glyphSize)
		{

			if (Encoding.UTF8.GetByteCount(text) != text.Length)
			{
				throw new ArgumentException("Only ASCII characters are supported.");
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			int buffLength = text.Length * 6 * (Vector3.SizeInBytes + Vector2.SizeInBytes);
			GL.BufferData(BufferTarget.ArrayBuffer, buffLength, (IntPtr)0, BufferUsageHint.StreamDraw);
			unsafe
			{
				float* ptr = (float*)GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly).ToPointer();
				int n = 0;
				//Foreach char add
				foreach (var c in text)
				{
					var UV = GetCharUV(c);
					var cPos = pos + (n++) * new Vector3(0.7f * glyphSize, 0.0f, 0.0f);
					WriteVertex(ref ptr, cPos,
						UV + new Vector2(0.2f * glyphUVSize, glyphUVSize));
					WriteVertex(ref ptr, cPos + new Vector3(0.8f * glyphSize, 0.0f, 0.0f),
						UV + new Vector2(0.8f * glyphUVSize, glyphUVSize));
					WriteVertex(ref ptr, cPos + new Vector3(0.8f * glyphSize, glyphSize, 0.0f),
						UV + new Vector2(0.8f * glyphUVSize, 0.0f));

					WriteVertex(ref ptr, cPos + new Vector3(0.8f * glyphSize, glyphSize, 0.0f),
						UV + new Vector2(0.8f * glyphUVSize, 0.0f));
					WriteVertex(ref ptr, cPos + new Vector3(0.0f, glyphSize, 0.0f),
						UV + new Vector2(0.2f * glyphUVSize, 0.0f));
					WriteVertex(ref ptr, cPos,
						UV + new Vector2(0.2f * glyphUVSize, glyphUVSize));
				}
				GL.UnmapBuffer(BufferTarget.ArrayBuffer);
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


			GL.BindVertexArray(VAO);
			shader.Bind();
			shader.SetUniform("proj", wView.Proj);
			shader.SetUniform("view", wView.View);
			shader.SetUniform("model", Matrix4.Identity);
			shader.SetUniform("col", color);

			GL.BindTexture(TextureTarget.Texture2D, fontAtlasTexID);

			GL.DrawArrays(PrimitiveType.Triangles, 0, 6 * text.Length);

			GL.BindTexture(TextureTarget.Texture2D, 0);
			shader.UnBind();
			GL.BindVertexArray(0);
		}
		static ShaderProgram BuildShader()
		{
			string vSource, fSource;
			using (var f = new StreamReader("Res/font.vert")) vSource = f.ReadToEnd();
			using (var f = new StreamReader("Res/font.frag")) fSource = f.ReadToEnd();
			return new ShaderProgram(vSource, fSource);
		}
		unsafe static void WriteVertex(ref float* ptr, Vector3 pos, Vector2 uv)
		{
			*(ptr++) = pos.X;
			*(ptr++) = pos.Y;
			*(ptr++) = pos.Z;
			*(ptr++) = uv.X;
			*(ptr++) = uv.Y;
		}
		static Vector2 GetCharUV(char c)
		{
			//Only ASCII
			Debug.Assert(c < 128);
			c -= (char)32;

			int row = c / charsPerRow;
			int col = c % charsPerRow;

			return new Vector2(col * tileDims / (float)atlasDims, row * tileDims / (float)atlasDims);
		}
		static int GenFontAtlas()
		{
			int fontAtlasTexID = 0;
			GL.GenTexture();
			var fontAtlas = new Bitmap("Res/font.png");
			Debug.Assert(fontAtlas.Width == atlasDims);
			Debug.Assert(fontAtlas.Height == atlasDims);

			GL.BindTexture(TextureTarget.Texture2D, fontAtlasTexID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			var bitmap = fontAtlas.LockBits(new Rectangle(0, 0, fontAtlas.Width, fontAtlas.Height),
											System.Drawing.Imaging.ImageLockMode.ReadOnly,
											System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, fontAtlas.Width, fontAtlas.Height,
						  0, PixelFormat.Rgba, PixelType.UnsignedByte, bitmap.Scan0);
			fontAtlas.UnlockBits(bitmap);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			return fontAtlasTexID;
		}
		const int tileDims = 32;
		const int atlasDims = 512;
		const int charsPerRow = atlasDims / tileDims;
		const float glyphUVSize = tileDims / (float)atlasDims;
		int VBO, VAO, IBO;
		readonly int fontAtlasTexID;
		IView wView;
		ShaderProgram shader;
	}
}
