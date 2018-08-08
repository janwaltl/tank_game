using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;

namespace Client.Graphics
{
	class ShaderProgram
	{
		public ShaderProgram(string vertexSource, string fragSource)
		{
			int vShader = BuildShader(vertexSource, ShaderType.VertexShader);
			int fShader = BuildShader(fragSource, ShaderType.FragmentShader);

			program = GL.CreateProgram();
			GL.AttachShader(program, vShader);
			GL.AttachShader(program, fShader);
			GL.LinkProgram(program);
			GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
			if (status != 0)
			{
				string log = GL.GetProgramInfoLog(program);
				//TODO throw
				throw new ArgumentException("Program error: " + log);
			}
		}

		private static int BuildShader(string source, ShaderType type)
		{
			int shader = GL.CreateShader(type);

			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);
			GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
			if (status != 0)
			{
				string log = GL.GetShaderInfoLog(shader);
				throw new ArgumentException("Shader error: " + log);
			}
			return shader;
		}

		readonly int program;
	}
}
