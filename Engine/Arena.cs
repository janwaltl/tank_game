using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenTK;
namespace Engine
{
	public class Arena
	{
		public enum CellType : byte
		{
			empty = 0,//floor
			wall = 1,
			spawn = 2,
			bonus = 3,
		}
		public struct Coords : IComparable<Coords>
		{
			public Coords(int x, int y) { this.x = x; this.y = y; }
			public int x;
			public int y;

			public int CompareTo(Coords other)
			{
				if (this.x < other.x)
					return -1;
				if (this.x > other.x)
					return +1;
				if (this.y < other.y)
					return -1;
				if (this.y > other.y)
					return +1;
				return 0;
			}
		}
		/// <summary>
		/// World coordinates of the [0,0] cell's center
		/// </summary>
		public static readonly Vector2 origin = new Vector2(0.0f, 0.0f);
		/// <summary>
		/// Dimensions of an cell
		/// </summary>
		public static readonly Vector2 boundingBox = new Vector2(1.0f, 1.0f);
		/// <summary>
		/// Vector ([1,1].Pos - [0,0].Pos) = in which direction the arena expands.
		/// Expressed in multiples of boundingBoxes, 1=meaning tightly packed cells, 2=one-cell space
		/// </summary>
		public static readonly Vector2 offset = new Vector2(+1.0f, +1.0f);
		/// <summary>
		/// Constructs empty arena optionally sorrounded by a wall.
		/// </summary>
		/// <param name="size">Length of arena's side</param>
		/// <param name="walls">Sorrounds the arena by a wall.</param>
		public Arena(int size, bool walls = false)
		{
			Size = size;
			grid = new CellType[size * size];
			if (walls)
				for (int i = 0; i < size; ++i)
				{
					grid[i] = CellType.wall;//Top
					grid[size * (size - 1) + i] = CellType.wall;//Bottom
					grid[size * i] = CellType.wall;//Left
					grid[size * i + size - 1] = CellType.wall;//Right
				}
			spawnPoints = new SortedSet<Coords>();
		}
		public CellType this[int x, int y]
		{
			get
			{
				CheckIndices(x, y);
				return grid[y * Size + x];
			}
			set
			{
				CheckIndices(x, y);
				//Update the spawn points.
				if (value == CellType.spawn)
					spawnPoints.Add(new Coords(x, y));
				else if(grid[y*Size+x]==CellType.spawn)
					spawnPoints.Remove(new Coords(x, y));
				grid[y * Size + x] = value;
			}
		}
		public IEnumerable<Coords> GetSpawnPoints()
		{
			return spawnPoints;
		}
		public static Arena FromFile(string filename)
		{
			using (var reader = new StreamReader(filename))
			{

				if (!int.TryParse(reader.ReadLine(), out int dims) || dims <= 0)
					throw new ArgumentException("Wrong arena file format.");

				var arena = new Arena(dims);
				for (int r = 0; r < dims; ++r)
				{
					string row = reader.ReadLine();
					if (row.Length != dims)
						throw new ArgumentException($"Line {r + 1} has invalid length.");
					for (int c = 0; c < dims; ++c)
					{
						CellType cell;
						switch (row[c])
						{
							case 'E':
								cell = CellType.empty; break;
							case 'W':
								cell = CellType.wall; break;
							case 'S':
								cell = CellType.spawn; break;
							case 'B':
								cell = CellType.bonus; break;
							default:
								throw new ArgumentException($"Line {r + 1} contains unrecognized char '{c}'.");
						}
						arena[c, r] = cell;
					}
				}
				return arena;
			}
		}
		/// <summary>
		/// Checks if indices are in correct range [0,Size-1] if not then throws ArgumentOutOfRangeException.
		/// </summary>
		/// <param name="x">Must be in [0,Size-1] or throws.</param>
		/// <param name="y">Must be in [0,Size-1] or throws.</param>
		public int Size { get; }
		private void CheckIndices(int x, int y)
		{
			if (x < 0 || x >= Size)
				throw new ArgumentOutOfRangeException(nameof(x) + $" is not valid, correct value is between 0 and {Size - 1}.");
			if (y < 0 || y >= Size)
				throw new ArgumentOutOfRangeException(nameof(y) + $" is not valid, correct value is between 0 and {Size - 1}.");
		}
		private CellType[] grid;
		private SortedSet<Coords> spawnPoints;
	}
}
