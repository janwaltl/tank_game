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

		public ClientUpdate(int playerID, PressedKeys keys, float mouseAngle, bool leftMouse, bool rightMouse, double dt)
		{
			PlayerID = playerID;
			Keys = keys;
			MouseAngle = mouseAngle;
			LeftMouse = leftMouse;
			RightMouse = rightMouse;
			DT = dt;
		}

		public static byte[] Encode(ClientUpdate u)
		{
			var bytes = new byte[4 + 1 + 4 + 8];
			var pID = Serialization.Encode(u.PlayerID);
			var mAngle = Serialization.Encode(u.MouseAngle);
			var left = Serialization.Encode(u.LeftMouse);
			var right = Serialization.Encode(u.RightMouse);
			var dt = Serialization.Encode(u.DT);
			int offset = 0;
			Array.Copy(pID, 0, bytes, offset, pID.Length);
			offset += pID.Length;
			bytes[offset] = (byte)u.Keys;
			offset += 1;
			Array.Copy(left, 0, bytes, offset, left.Length);
			offset += left.Length;
			Array.Copy(right, 0, bytes, offset, right.Length);
			offset += right.Length;
			Array.Copy(mAngle, 0, bytes, offset, mAngle.Length);
			offset += mAngle.Length;
			Array.Copy(dt, 0, bytes, offset, dt.Length);
			offset += dt.Length;
			return bytes;
		}
		public static ClientUpdate Decode(byte[] bytes)
		{
			int pID = Serialization.DecodeInt(bytes, 0);
			PressedKeys keys = (PressedKeys)bytes[4];
			bool left = Serialization.DecodeBool(bytes, 5);
			bool right = Serialization.DecodeBool(bytes, 6);
			float mAngle = Serialization.DecodeFloat(bytes, 7);
			double dt = Serialization.DecodeDouble(bytes, 11);
			return new ClientUpdate(pID, keys, mAngle, left, right, dt);
		}

		public int PlayerID { get; }
		public PressedKeys Keys { get; }

		public bool LeftMouse { get; }
		public bool RightMouse { get; }
		public float MouseAngle { get; }
		public double DT { get; }
	}
}
