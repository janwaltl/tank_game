using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
	/// <summary>
	/// Represent an update message sent by client to the server while playing.
	/// </summary>
	public class ClientUpdate
	{
		[Flags]
		public enum PressedKeys : byte
		{
			None = 1 << 0,
			W = 1 << 1,
			A = 1 << 2,
			S = 1 << 3,
			D = 1 << 4,
		}

		public ClientUpdate(int playerID, PressedKeys keys, double dt)
		{
			PlayerID = playerID;
			Keys = keys;
			DT = dt;
		}

		public static byte[] Encode(ClientUpdate u)
		{
			var bytes = new byte[4 + 4 + 8];

			var pID = Serialization.Encode(u.PlayerID);
			Array.Copy(pID, 0, bytes, 0, pID.Length);

			bytes[4] = (byte)u.Keys;

			var dt = Serialization.Encode(u.DT);
			Array.Copy(dt, 0, bytes, 5, dt.Length);
			return bytes;
		}
		public static ClientUpdate Decode(byte[] bytes)
		{
			int pID = Serialization.DecodeInt(bytes, 0);
			PressedKeys keys = (PressedKeys)bytes[4];
			double dt = Serialization.DecodeDouble(bytes, 5);
			return new ClientUpdate(pID, keys, dt);
		}

		public int PlayerID { get; }
		public PressedKeys Keys { get; }
		public double DT { get; }
	}
}
