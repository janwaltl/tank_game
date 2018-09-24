using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Client.Playing
{
	/// <summary>
	/// Class that can render an quad.
	/// </summary>
	class QuadGenerator
	{
		/// <summary>
		/// Generates VBO for an unit quad vec3 points and sets the position,uv as the attributes at given locations.
		/// </summary>
		/// <param name="posAttribLoc">To which attribute location bind the position of the quad.</param>
		/// <param name="posAttribLoc">To which attribute location bind the UF coordinates of the quad.</param>
		/// <returns>VBO containing the quad</returns>
		public static int GenQuadVBO(int posAttribLoc, int uvAttribLoc)
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
			int VBO = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
			int quadLen = quad.Length * Vector3.SizeInBytes;
			int uvLen = uv.Length * Vector2.SizeInBytes;
			int buffLen = quadLen + uvLen;
			GL.BufferData(BufferTarget.ArrayBuffer, buffLen, (IntPtr)0, BufferUsageHint.StaticDraw);
			GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)0, (IntPtr)quadLen, quad);
			GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)quadLen, (IntPtr)uvLen, uv);
			GL.VertexAttribPointer(posAttribLoc, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);//Position
			GL.VertexAttribPointer(uvAttribLoc, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, quadLen);//UV
			GL.EnableVertexAttribArray(posAttribLoc);
			GL.EnableVertexAttribArray(uvAttribLoc);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			return VBO;
		}
		/// <summary>
		/// Generates IBO for the quad to be rendered with GL_TRIANGLE_FAN.
		/// </summary>
		/// <returns>IBO containing the indices.</returns>
		public static int GenQuadIBO()
		{
			int IBO = GL.GenBuffer();
			var indices = new uint[] { 0, 1, 2, 3 };
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, IBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);
			return IBO;
		}
	}
}
