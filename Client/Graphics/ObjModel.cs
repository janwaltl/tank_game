using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Client.Graphics
{
	/// <summary>
	/// Class representing loaded .obj model.
	/// </summary>
	class ObjModel
	{
		/// <summary>
		/// Triangle represented as 3 indices to 'vertices' array.
		/// </summary>
		public struct Face
		{
			/// <summary>
			/// Indices to 'vertices' array that generate this face.
			/// </summary>
			public int v0, v1, v2;
		}
		public class Material
		{
			/// <summary>
			/// Range (inclusive) of indices to 'faces' array with this material.
			/// </summary>
			public int first, last;
			/// <summary>
			/// Name of the material.
			/// </summary>
			public string name;
		}
		/// <summary>
		/// Loads models from .obj file. Throws arguments exception if the .obj format is not correct
		/// </summary>
		/// <param name="filename">FIle from which to load.</param>
		/// <returns></returns>
		public static List<ObjModel> FromFile(string filename)
		{
			var models = new List<ObjModel>();
			var currModel = new ObjModel();
			int startIndex = 1;//.obj starts indexing vertices from one.
			using (var f = new StreamReader(filename))
			{
				string line;
				while ((line = f.ReadLine()) != null)
				{
					if (line.Length == 0)
						continue;

					if (line[0] != 'o')//Model
						throw new ArgumentException("Corrupted model.");
					models.Add(ReadModel(line.Substring(2), f, startIndex));
					//Because vertices are numbered file-wise -> this makes it object-wise.
					startIndex += models[models.Count - 1].vertices.Count;
				}
			}
			return models;
		}
		/// <summary>
		/// Reads one model from the stream. Currently accepts only vertices followed by faces with optional materials.
		/// </summary>
		/// <param name="name">Name of the model</param>
		/// <param name="reader">Source of the data.</param>
		/// <param name="startIndex">Index of the first vertex found.</param>
		/// <returns></returns>
		static ObjModel ReadModel(string name, StreamReader reader, int startIndex)
		{
			var vertices = new List<Vector3>();
			//Read vertices
			while (reader.Peek() == 'v')
			{
				var tokens = reader.ReadLine().Split(' ');
				if (tokens.Length != 4 ||//'v' + 3 floats
					!float.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) ||
					!float.TryParse(tokens[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float y) ||
					!float.TryParse(tokens[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float z))
					throw new ArgumentException("Corrupted model.");
				vertices.Add(new Vector3(x, y, z));
			}
			var faces = new List<Face>();
			var materials = new List<Material>();
			int c = reader.Peek();
			Material currMat = null;
			while (c == 'u' || c == 'f')
			{
				if (c == 'u')
				{
					var tokens = reader.ReadLine().Split(' ');
					if (tokens.Length != 2)//'usemtl' + name
						throw new ArgumentException("Corrupted model.");
					if (currMat != null)
					{
						//Mark the last face with this material.
						currMat.last = faces.Count - 1;
						materials.Add(currMat);
					}
					currMat = new Material { name = tokens[1], first = faces.Count };
				}
				else
				{
					var tokens = reader.ReadLine().Split(' ');
					if (tokens.Length != 4 ||//'v' + 3 ints
						!int.TryParse(tokens[1], out int v0) ||
						!int.TryParse(tokens[2], out int v1) ||
						!int.TryParse(tokens[3], out int v2))
						throw new ArgumentException("Corrupted model.");
					faces.Add(new Face { v0 = v0 - startIndex, v1 = v1 - startIndex, v2 = v2 - startIndex });
				}
				c = reader.Peek();
			}
			//Mark the last face with this material.
			if (currMat != null)
			{
				currMat.last = faces.Count - 1;
				materials.Add(currMat);
			}
			return new ObjModel { name = name, vertices = vertices, faces = faces, materials = materials };
		}
		public string name;
		public List<Vector3> vertices;
		public List<Face> faces;
		public List<Material> materials;
	}
}