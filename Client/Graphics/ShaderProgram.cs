using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Client.Graphics
{
	/// <summary>
	/// Represents OpenGL shader program.
	/// </summary>
	class ShaderProgram:IDisposable
	{
		public ShaderProgram(string vertexSource, string fragSource)
		{
			int vShader = BuildShader(vertexSource, ShaderType.VertexShader);
			int fShader = BuildShader(fragSource, ShaderType.FragmentShader);

			program = GL.CreateProgram();
			GL.AttachShader(program, vShader);
			GL.AttachShader(program, fShader);
			GL.LinkProgram(program);
			GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
			if (linkStatus == 0)
			{
				string log = GL.GetProgramInfoLog(program);
				throw new ArgumentException("Program error: " + log);
			}
			GL.DetachShader(program, vShader);
			GL.DetachShader(program, fShader);
			GL.DeleteShader(vShader);
			GL.DeleteShader(fShader);
			activeUniforms = queryUniforms(program);
		}
		/// <summary>
		/// Sets passed uniform to given value.
		/// </summary>
		/// <param name="name">name of the active mat4 uniform in the shader</param>
		/// <param name="mat">desired value.</param>
		public void SetUniform(string name, Matrix4 mat)
		{
			GL.UniformMatrix4(GetUniformLoc(name, ActiveUniformType.FloatMat4), false, ref mat);
		}
		public void SetUniform(string name, Vector3 vec)
		{
			GL.Uniform3(GetUniformLoc(name, ActiveUniformType.FloatVec3), ref vec);
		}
		/// <summary>
		/// Sets named uniform sampler to given texture unit.
		/// </summary>
		public void SetUniformSampler2D(string name, int unit)
		{
			GL.Uniform1(GetUniformLoc(name, ActiveUniformType.Sampler2D), unit);
		}
		public void Bind()
		{
			GL.UseProgram(program);
		}
		public void UnBind()
		{
			GL.UseProgram(0);
		}
		/// <summary>
		/// Returns location of active uniform with given name. If an active uniform with that name doesn't exists
		/// or isn't the right type then throws ArgumentException.
		/// </summary>
		/// <returns>Location of named uniform.</returns>
		private int GetUniformLoc(string name, ActiveUniformType type)
		{
			if (!activeUniforms.TryGetValue(name, out Uniform u))
				throw new ArgumentException($"'{name}' is not an active uniform");
			else if (u.type != type)
				throw new ArgumentException($"'{name}' is not {type} but {u.type}");
			else
				return u.location;
		}
		/// <summary>
		/// Creates and compiles OpenGL shader and returns its ID.
		/// </summary>
		/// <returns>ID of the new shader</returns>
		private static int BuildShader(string source, ShaderType type)
		{
			int shader = GL.CreateShader(type);

			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);
			GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
			if (status == 0)
			{
				string log = GL.GetShaderInfoLog(shader);
				throw new ArgumentException("Shader error: " + log);
			}
			return shader;
		}
		/// <summary>
		/// Returns active uniforms of a linked program.
		/// </summary>
		/// <param name="program">Successfully linked program.</param>
		/// <returns>List of active uniforms.</returns>
		private static Dictionary<string, Uniform> queryUniforms(int program)
		{
			Dictionary<string, Uniform> uniforms = new Dictionary<string, Uniform>();
			GL.GetProgram(program, GetProgramParameterName.ActiveAttributes, out int numAttribs);
			GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out int numUniforms);

			for (int n = 0; n < numUniforms; ++n)
			{
				string name = GL.GetActiveUniform(program, n, out int size, out ActiveUniformType type);
				int loc = GL.GetUniformLocation(program, name);
				uniforms.Add(name, new Uniform { location = loc, type = type });
			}
			return uniforms;
		}

		public void Dispose()
		{
			GL.DeleteProgram(program);
		}

		/// <summary>
		/// Represents uniform varaible in shaders.
		/// </summary>
		struct Uniform
		{
			public int location;
			public ActiveUniformType type;
		}
		readonly Dictionary<string, Uniform> activeUniforms;
		readonly int program;
	}
}
