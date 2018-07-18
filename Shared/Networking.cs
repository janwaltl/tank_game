using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Contains shared classes between the Server and client.
/// Mostly communication oriented.
/// </summary>
namespace Shared
{
	/// <summary>
	/// Represent an update message sent by client to the server while playing
	/// </summary>
	public class ClientUpdate
	{
		public ClientUpdate(string msg) { this.msg = msg; }
		//CURRENTLY just a string
		public string msg;

		public static byte[] Encode(ClientUpdate update)
		{
			return Encoding.BigEndianUnicode.GetBytes(update.msg);
		}
		public static ClientUpdate Decode(byte[] bytes)
		{
			string msg = Encoding.BigEndianUnicode.GetString(bytes);
			return new ClientUpdate(msg);
		}

	}
	public class ClientConnecting
	{
		public int playerID;
		public string testMsg;
		public ClientConnecting(int playerID, string testMsg)
		{
			this.playerID = playerID; this.testMsg = testMsg;
		}
		public static byte[] Encode(ClientConnecting c)
		{
			var msgBytes = Encoding.BigEndianUnicode.GetBytes(c.testMsg);
			var pBytes = BitConverter.GetBytes(c.playerID);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(pBytes);
			var msg = new byte[msgBytes.Length + pBytes.Length];
			Array.Copy(pBytes, msg, pBytes.Length);
			Array.Copy(msgBytes, 0, msg, pBytes.Length, msgBytes.Length);
			return msg;
		}
		public static ClientConnecting Decode(byte[] bytes, int startIndex)
		{
			int playerID = BitConverter.ToInt32(bytes, startIndex);
			string msg = Encoding.BigEndianUnicode.GetString(bytes, startIndex + 4, bytes.Length - startIndex - 4);
			return new ClientConnecting(playerID, msg);
		}
		public static ClientConnecting Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		//Message structure:
		//4					Bytes playerID
		//Rest				Test Message
	}

	public static class Serialization
	{
		/// <summary>
		/// Preprends the array with 4byte int length
		/// </summary>
		public static byte[] PrependLength(byte[] bytes)
		{
			byte[] len = Encode(bytes.Length);
			byte[] res = new byte[bytes.Length + len.Length];
			Array.Copy(len, 0, res, 0, len.Length);
			Array.Copy(bytes, 0, res, len.Length, bytes.Length);
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

