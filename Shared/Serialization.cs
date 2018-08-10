using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
	public static class Serialization
	{
		/// <summary>
		/// Preprends the array with 4byte int length
		/// </summary>
		public static byte[] PrependLength(byte[] bytes)
		{
			return CombineArrays(Encode(bytes.Length), bytes);
		}
		public static byte[] StripLength(byte[] bytesWithLength)
		{
			Debug.Assert(bytesWithLength.Length >= 4);
			byte[] res = new byte[bytesWithLength.Length - 4];
			Array.Copy(bytesWithLength, 4, res, 0, bytesWithLength.Length - 4);
			return res;
		}
		public static byte[] CombineArrays(byte[] first, byte[] second)
		{
			var res = new byte[first.Length + second.Length];
			Array.Copy(first, res, first.Length);
			Array.Copy(second, 0, res, first.Length, second.Length);
			return res;
		}
		public static byte[] Encode(int x)
		{
			var bytes = BitConverter.GetBytes(x);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}
		public static int DecodeInt(byte[] bytes, int startIndex)
		{
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes, startIndex, 4);
			return BitConverter.ToInt32(bytes, 0);
		}
	}
}
