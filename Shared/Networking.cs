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
	public static class Ports
	{
		public const int serverConnection = 23545;
		public const int clientUpdates = 23546;
		public const int serverUpdates = 23547;
	}

	/// <summary>
	/// Data sent from server to a client when the client connects.
	/// </summary>
	public class ConnectingStaticData
	{
		public int playerID;
		public string testMsg;
		public ConnectingStaticData(int playerID, string testMsg)
		{
			this.playerID = playerID; this.testMsg = testMsg;
		}
		public static byte[] Encode(ConnectingStaticData c)
		{
			var msgBytes = Encoding.BigEndianUnicode.GetBytes(c.testMsg);
			var pBytes = Serialization.Encode(c.playerID);
			return Serialization.CombineArrays(pBytes, msgBytes);
		}
		public static ConnectingStaticData Decode(byte[] bytes, int startIndex)
		{
			int playerID = Serialization.DecodeInt(bytes, startIndex);
			string msg = Encoding.BigEndianUnicode.GetString(bytes, startIndex + 4, bytes.Length - startIndex - 4);
			return new ConnectingStaticData(playerID, msg);
		}
		public static ConnectingStaticData Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
		//Message structure:
		//4					Bytes playerID
		//Rest				Test Message
	}
	/// <summary>
	/// Represents dynamic data about the game.
	/// Sent from server to the player when player signals they are ready while connecting.
	/// </summary>
	public class ConnectingDynamicData
	{
		public string testData;

		public ConnectingDynamicData(string testData)
		{
			this.testData = testData;
		}
		public static byte[] Encode(ConnectingDynamicData d)
		{
			return Encoding.BigEndianUnicode.GetBytes(d.testData);
		}
		public static ConnectingDynamicData Decode(byte[] bytes, int startIndex)
		{
			return new ConnectingDynamicData(Encoding.BigEndianUnicode.GetString(bytes, startIndex, bytes.Length - startIndex));
		}
		public static ConnectingDynamicData Decode(byte[] bytes)
		{
			return Decode(bytes, 0);
		}
	}
	public static class TaskExtensions
	{
		/// <summary>
		/// Do not wait for the task.
		/// </summary>
		/// <param name="t"></param>
		public static void Detach(this Task t)
		{
			//Only forget launched tasks for now.
			Debug.Assert(t.Status != TaskStatus.Created);
		}
	}
}

