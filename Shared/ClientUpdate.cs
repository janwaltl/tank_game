using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine;
using OpenTK;

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

		public ClientUpdate(int ID, int playerID, PressedKeys keys, float mouseAngle, bool leftMouse, bool rightMouse, double dt)
		{
			this.ID = ID;
			PlayerID = playerID;
			Keys = keys;
			MouseAngle = mouseAngle;
			LeftMouse = leftMouse;
			RightMouse = rightMouse;
			DT = dt;
		}

		public static byte[] Encode(ClientUpdate u)
		{
			var ID = Serialization.Encode(u.ID);
			var pID = Serialization.Encode(u.PlayerID);
			var mAngle = Serialization.Encode(u.MouseAngle);
			var left = Serialization.Encode(u.LeftMouse);
			var right = Serialization.Encode(u.RightMouse);
			var dt = Serialization.Encode(u.DT);
			var length = ID.Length + pID.Length + mAngle.Length + 1 + left.Length + right.Length + dt.Length;
			var bytes = new byte[length];
			int offset = 0;
			Array.Copy(ID, 0, bytes, offset, ID.Length);
			offset += ID.Length;
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
		public static ClientUpdate Decode(byte[] bytes, int offset = 0)
		{
			int ID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			int pID = Serialization.DecodeInt(bytes, offset);
			offset += 4;
			PressedKeys keys = (PressedKeys)bytes[offset];
			offset += 1;
			bool left = Serialization.DecodeBool(bytes, offset);
			offset += 1;
			bool right = Serialization.DecodeBool(bytes, offset);
			offset += 1;
			float mAngle = Serialization.DecodeFloat(bytes, offset);
			offset += 4;
			double dt = Serialization.DecodeDouble(bytes, offset);
			offset += 8;
			return new ClientUpdate(ID, pID, keys, mAngle, left, right, dt);
		}

		/// <summary>
		/// Generates engine command to move the player according to pressed keys. 
		/// </summary>
		public PlayerAccCmd GenPlayerMovement()
		{
			Vector3 deltaVel = new Vector3(0.0f);
			if ((Keys & PressedKeys.W) != 0)
				deltaVel += new Vector3(0.0f, 1.0f, 0.0f);
			if ((Keys & PressedKeys.S) != 0)
				deltaVel += new Vector3(0.0f, -1.0f, 0.0f);
			if ((Keys & PressedKeys.A) != 0)
				deltaVel += new Vector3(-1.0f, 0.0f, 0.0f);
			if ((Keys & PressedKeys.D) != 0)
				deltaVel += new Vector3(1.0f, 0.0f, 0.0f);
			return new PlayerAccCmd(PlayerID, deltaVel * (float)DT * Player.speed);
		}
		/// <summary>
		/// Generates an engine command that rotates the tank's tower based on mouse position.
		/// </summary>
		public PlayerTowerCmd GenTowerCmd()
		{
			return new PlayerTowerCmd(PlayerID, MouseAngle);
		}
		public int ID { get; }
		public int PlayerID { get; }
		public PressedKeys Keys { get; }

		public bool LeftMouse { get; }
		public bool RightMouse { get; }
		public float MouseAngle { get; }
		public double DT { get; }
	}
}
