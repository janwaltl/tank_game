﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
namespace Engine
{
	public class Arena
	{
		public enum CellType : byte
		{
			empty,//floor
			wall,
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
