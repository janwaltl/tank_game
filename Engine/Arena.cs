using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	public class Arena
	{
		public enum CellType
		{
			empty,//floor
			wall,
		}
		/// <summary>
		/// Constructs empty arena sorrounded with walls
		/// </summary>
		/// <param name="size">Length of arena's side</param>
		public Arena(int size)
		{
			Size = size;
			grid = new CellType[size * size];
			for (int i = 0; i < size; ++i)
			{
				grid[i] = CellType.wall;//Top
				grid[size * (size - 1) + i] = CellType.wall;//Bottom
				grid[size * i] = CellType.wall;//Left
				grid[size * i + size - 1] = CellType.wall;//Right
			}
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
				grid[y * Size + x] = value;
			}
		}
		/// <summary>
		/// Checks if indices are in correct range [0,Size-1] if not then throws ArgumentOutOfRangeException.
		/// </summary>
		/// <param name="x">Must be in [0,Size-1] or throws.</param>
		/// <param name="y">Must be in [0,Size-1] or throws.</param>
		private void CheckIndices(int x, int y)
		{
			if (x < 0 || x >= Size)
				throw new ArgumentOutOfRangeException(nameof(x) + $" is not valid, correct value is between 0 and {Size - 1}.");
			if (y < 0 || y >= Size)
				throw new ArgumentOutOfRangeException(nameof(y) + $" is not valid, correct value is between 0 and {Size - 1}.");
		}
		public int Size { get; }
		private CellType[] grid;
	}
}
